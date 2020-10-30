using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp1.Common;
using WindowsFormsApp1.DataSet1TableAdapters;
using WindowsFormsApp1.Enums;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private OleDbConnection con;
        private OleDbCommand cmd;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadEmployeeList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //con.Close();
            //var bl = new BindingList<User>(studentList[53]);
            ////int index = this.dataGridView1.Rows.Add();

            //DataTable dt1 = new DataTable();

            //DataColumn c1 = new DataColumn("UserId");
            //dt1.Columns.Add(c1);
            //DataColumn c2 = new DataColumn("InTime");
            //dt1.Columns.Add(c2);
            //DataColumn c3 = new DataColumn("OutTime");
            //dt1.Columns.Add(c3);




            //for (int i = 1; i <= bl.Count; i++)
            //{
            //    DataRow dr = dt1.NewRow();
            //    dr["UserId"] = bl[i].Id;
            //    dr["InTime"] = bl[i].CheckInDatetime;
            //    dr["OutTime"] = bl[i].CheckOutDateTime;
            //    dt1.Rows.Add(dr);

            //    dataGridView1.DataSource = dt1;
            //}


            if (this.employeesComboBox.SelectedValue == null ||
                (int)this.employeesComboBox.SelectedValue == 0)
            {
                //TODO: generate appropriate msg
                return;
            }

            string selectedEmployee = this.employeesComboBox.Text.ToString();

            var fromDate = this.fromDateTimePicker.Value.ToString("d", DateTimeFormatInfo.InvariantInfo);
            var toDate = this.toDateTimePicker.Value.ToString("d", DateTimeFormatInfo.InvariantInfo);

            var query = @"SELECT [User].Name, [CK].CHECKTIME, [CK].CHECKTYPE, [User].DEFAULTDEPTID
                            FROM CHECKINOUT [CK] 
                        LEFT JOIN USERINFO [User] ON  [CK].USERID = [User].USERID
                            WHERE([User].Name = '{0}')
                                AND(([CK].CHECKTIME >= #{1}#) AND ([CK].CHECKTIME < #{2}#))";


            using (this.con = new OleDbConnection(GlobalConstant.ConnStr))
            {
                this.cmd = new OleDbCommand(string.Format(query, selectedEmployee, fromDate, toDate), con);
                this.con.Open();

                OleDbDataReader dr = cmd.ExecuteReader();

                List<User> users = new List<User>();
                string newDate = new DateTime(1900, 1, 1).ToString("dd.mm.yyyy");


                while (dr.Read())
                {
                    var eymployeeName = dr.GetString(dr.GetOrdinal("Name"));
                    var departmentId = dr.GetValue(dr.GetOrdinal("DEFAULTDEPTID")).ToString();
                    var checkType = dr.GetString(dr.GetOrdinal("CHECKTYPE"));
                    var getDateString = dr.GetValue(dr.GetOrdinal("CHECKTIME")).ToString();
                    
                    DateTime dt = DateTime.Parse(getDateString);
                    string currentDate = dt.Date.ToString("dd.MM.yyyy");
                    string currentTime = dt.ToString("HH:mm:ss");

                    bool isCurrentDate = newDate.Equals(currentDate);

                    //TODO: make a better code here-users????? list of user details
                    var user = users.FirstOrDefault(u => u.Name == eymployeeName);
                    
                    //New user, new day, first row!
                    if (user is null && !isCurrentDate)
                    {
                        user = new User() { Name = eymployeeName };

                        newDate = currentDate;
                        user.CheckDate = currentDate;

                        SetScheduleTimeByDepartment(user, departmentId);
                        SetTimeUser(user, checkType, currentTime);
                        TimeViolation(user);

                        users.Add(user);
                    }
                    //same user, same date
                    else if(isCurrentDate)
                    {
                        SetTimeUser(user, checkType, currentTime);
                        TimeViolation
                    }


                    //if (string.IsNullOrEmpty(user.ScheduleIn) ||
                    //    string.IsNullOrEmpty(user.ScheduleOut))
                    //{
                    //    SetScheduleTimeByDepartment(user, departmentId);
                    //}

                    //if (!isNewDate)
                    //{
                    //    newDate = currentDate;
                    //    user.CheckDate = currentDate;

                    //    SetTimeUser(user, checkType, currentTime);
                    //    users.Add(user);
                    //}
                    //else
                    //{
                    //    SetTimeUser(user, checkType, currentTime);                        
                    //}
                    
                }

                dr.Close();
            }

            //Fill from query datagrid
            //using (this.con = new OleDbConnection(ConnStr))
            //{

            //    this.cmd = new OleDbCommand(string.Format(query, selectedEmployee, fromDate, toDate), con);
            //    this.con.Open();

            //    cmd.CommandType = CommandType.Text;
            //    OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            //    DataTable scores = new DataTable();
            //    da.Fill(scores);
            //    dataGridView1.DataSource = scores;
            //}
        }

        private void SetScheduleTimeByDepartment(User user, string departmentId)
        {
            var dept = (Department)Enum.Parse(typeof(Department), departmentId, true);

            switch (dept)
            {
                case Department.Service:
                case Department.ServiceEntrance:
                case Department.CarWash:
                case Department.PaintCrashService:
                case Department.GTP:
                    user.ScheduleIn = GlobalConstant.startWorkTimeService;
                    user.ScheduleOut = GlobalConstant.finishWorkTime;
                    break;
                case Department.Мerchant:
                    user.ScheduleIn = GlobalConstant.startWorkTimeМerchant;
                    user.ScheduleOut = GlobalConstant.finishWorkTime;
                    break;
            }
        }

        private void SetTimeUser(User user, string checkType, string currentTime)
        {

            if (checkType.Equals("I"))
            {
                user.CheckTimeIn = currentTime;
            }
            else
            {
                user.CheckTimeOut = currentTime;
            }
        }

        private void TimeViolation(string schedulerTime, string T)
        {
            //TimeSpan lateTime = DateTime.Parse(user.CheckTimeIn).Subtract(DateTime.Parse(user.ScheduleIn));
            //TimeSpan earlyTime = DateTime.Parse(user.ScheduleOut).Subtract(DateTime.Parse(user.CheckTimeOut));

            //user.LateTimeIn = lateTime.ToString();
            //user.EarlyTimeOut = earlyTime.ToString();
        }

        private void LoadEmployeeList()
        {
            string query = @"SELECT USERID, Name  
                            FROM USERINFO 
                            ORDER BY Name";

            using (this.con = new OleDbConnection(GlobalConstant.ConnStr))
            {
                this.con.Open();
                this.cmd = new OleDbCommand(query, con);

                OleDbDataReader rd = this.cmd.ExecuteReader();

                DataTable dt = new DataTable();
                dt.Load(rd);

                DataRow row = dt.NewRow();
                row[0] = 0;
                row[1] = "Please select";
                dt.Rows.InsertAt(row, 0);

                this.employeesComboBox.DataSource = dt;
                this.employeesComboBox.DisplayMember = "Name";
                this.employeesComboBox.ValueMember = "USERID";
            }
        }


    }
}
