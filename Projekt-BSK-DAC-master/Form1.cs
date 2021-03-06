﻿using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        DBConnection connection;
        ArrayList task;
        String chosenTable;


        CancellationTokenSource ts;
        CancellationToken ct;

        public Form1()
        {
            InitializeComponent();


            // wyświetlanie loginform i pobranie nowego połaćzenia
            LoginForm form = new LoginForm();
            form.ShowDialog();
            connection = form.connection;



            if (connection != null)
            {
                toolStripStatusLabel1.Text = "Jesteś zalogowany jako " + connection.Login;
                connection.ListTabels = connection.GetTablesName();
                DelegateMethod(connection.GetTablesName());

                task = new ArrayList();

                //foreach (var i in connection.ListTabels)
                string i = "Autor";
                    task.Add( Task.Factory.StartNew(() => chceckIfPrivilageChange(i, ct)));
                    //Task task = Task.Run(() => chceckIfPrivilageChange("ksiazka"));
            }
        }

        private void chceckIfTablesChange()
        {
            while (true)
            {
                var list = connection.GetTablesName();
                if (dataGridView1.InvokeRequired)
                {
                    if (connection.comparisonTabels(list))
                    {
                        connection.ListTabels = list;
                        dataGridView1.BeginInvoke((new MyDelegate(DelegateMethod)), list);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void chceckIfPrivilageChange(String nameTable, CancellationToken ct)
        {

            while (true)
            {
               // if (chosenTable == nameTable)
               // {
                var list = connection.GetTablePrivilegesAllUsers(chosenTable);
                    if (dataGridView2.InvokeRequired)
                    {
                        if (connection.isGranteeListTheSame(list))
                        {
                            connection.ListGrantee = list;
                            dataGridView2.BeginInvoke((new Delegate(refreshPreviligeTabele)), list);
                        }
                    }
                    Thread.Sleep(2000);
                    if (ct.IsCancellationRequested)
                    {
                        // another thread decided to cancel
                        Console.WriteLine("task canceled");
                        break;
                    }
                //}
            }
        }

        public delegate void MyDelegate(List<String> myControl);

        public void DelegateMethod(List<String> list)
        {
            var rem = 0;
            //rem = dataGridView1.SelectedCells[0].RowIndex;
            //dataGridView1.Rows[rem].Selected = false;       

            dataGridView1.Rows.Clear();
            foreach (var tabel in list)
            {
                dataGridView1.Rows.Add(tabel);
            }


            //dataGridView1.Rows[rem].Selected = true;
            dataGridView1.CurrentCell = dataGridView1[0, rem];
            //dataGridView1_CellClick(dataGridView1, new DataGridViewCellEventArgs(0, 0));

        }

        public delegate void Delegate(List<Grantee> myControl);

        private void refreshPreviligeTabele(List<Grantee> list)
        {
            dataGridView2.Rows.Clear();
            dataGridView2.Refresh();

            connection.ListGrantee = list;
            foreach (var tabel in list)
            {
                dataGridView2.Rows.Add(tabel.UserName, tabel.Select, tabel.SelectIsGrantable, tabel.Insert, tabel.InsertIsGrantable, tabel.Delete, tabel.DeleteIsGrantable, tabel.Update, tabel.UpdateIsGrantable, tabel.TakeOver, tabel.TakeOverIsGrantable);
            }
        }
        //nadawanie uprawnień
        private void grant(string privilage, bool grantable)
        {
            var granteeName = "'" + dataGridView2.CurrentCell.Value.ToString() + "'@'%'";
            var tableName = dataGridView1.CurrentCell.Value.ToString();
            string connectionString = string.Format("Server={0}; Port={1}; database={2}; UID={3}; password={4};", connection.Server, connection.Port, connection.DatabaseName, connection.Login, connection.Password);
            MySqlConnection myConnection = new MySqlConnection(connectionString);
            MySqlCommand cmd = myConnection.CreateCommand();
            if (privilage == "TAKEOVER")
            {
                if (!grantable)
                    foreach(DataGridViewRow row in dataGridView1.Rows)
                    {
                        myConnection.Open();
                        cmd.CommandText = string.Format("INSERT INTO uprawnienia.user_privileges VALUES(\"{0}\", '{1}', '{2}', 'NO', '{3}');", granteeName, row.Cells[0].Value.ToString(), privilage, connection.Login);
                        cmd.ExecuteReader();
                        myConnection.Close();
                    }
                else
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        myConnection.Open();
                        cmd.CommandText = string.Format("INSERT INTO uprawnienia.user_privileges VALUES(\"{0}\", '{1}', '{2}', 'YES', '{3}');", granteeName, row.Cells[0].Value.ToString(), privilage, connection.Login);
                        cmd.ExecuteReader();
                        myConnection.Close();
                    }
                return;
            }
            myConnection.Open();
            if (!grantable)
                cmd.CommandText = string.Format("INSERT INTO uprawnienia.user_privileges VALUES(\"{0}\", '{1}', '{2}', 'NO', '{3}');",granteeName, tableName, privilage, connection.Login);
            else
                cmd.CommandText = string.Format("INSERT INTO uprawnienia.user_privileges VALUES(\"{0}\", '{1}', '{2}', 'YES', '{3}');", granteeName, tableName, privilage, connection.Login);
            cmd.ExecuteReader();
            myConnection.Close();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            bool giveGrantable = false;
            if (checkBox1.Checked && connection.myPrivileges.Insert.ToString() == "True")
            {
                if (checkBox5.Checked && connection.myPrivileges.InsertIsGrantable.ToString() == "True")
                    giveGrantable = true;
                else
                    giveGrantable = false;
                grant("INSERT", giveGrantable);
            }
            if (checkBox2.Checked && connection.myPrivileges.Delete.ToString() == "True")
            {
                if (checkBox6.Checked && connection.myPrivileges.DeleteIsGrantable.ToString() == "True")
                    giveGrantable = true;
                else
                    giveGrantable = false;
                grant("DELETE", giveGrantable);
            }
            if (checkBox3.Checked && connection.myPrivileges.Update.ToString() == "True")
            {
                if (checkBox7.Checked && connection.myPrivileges.UpdateIsGrantable.ToString() == "True")
                    giveGrantable = true;
                else
                    giveGrantable = false;
                grant("UPDATE", giveGrantable);
            }
            if (checkBox4.Checked && connection.myPrivileges.Select.ToString() == "True")
            {
                if (checkBox8.Checked && connection.myPrivileges.SelectIsGrantable.ToString() == "True")
                    giveGrantable = true;
                else
                    giveGrantable = false;
                grant("SELECT", giveGrantable);
            }
            if(checkBox9.Checked && connection.myPrivileges.TakeOver.ToString() == "True")
            {
                if (checkBox10.Checked && connection.myPrivileges.TakeOverIsGrantable.ToString() == "True")
                {
                    giveGrantable = true;
                }
                else
                    giveGrantable = false;
                grant("TAKEOVER", giveGrantable);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.AllowUserToAddRows = false;
            dataGridView2.AllowUserToAddRows = false;
            dataGridView1.MultiSelect = false;
            dataGridView2.MultiSelect = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(connection.IsConnect())
                connection.Close();
        }
        private void disableAllCheckboxes()
        {
            checkBox1.Enabled = false;
            checkBox1.Checked = false;

            checkBox2.Enabled = false;
            checkBox2.Checked = false;

            checkBox3.Enabled = false;
            checkBox3.Checked = false;

            checkBox4.Enabled = false;
            checkBox4.Checked = false;

            checkBox5.Enabled = false;
            checkBox5.Checked = false;

            checkBox6.Enabled = false;
            checkBox6.Checked = false;

            checkBox7.Enabled = false;
            checkBox7.Checked = false;

            checkBox8.Enabled = false;
            checkBox8.Checked = false;

            checkBox9.Enabled = false;
            checkBox9.Checked = false;

            checkBox10.Enabled = false;
            checkBox10.Checked = false;

            button2.Enabled = false;

        }
        private void uncheck()
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
            checkBox7.Checked = false;
            checkBox8.Checked = false;
            checkBox9.Checked = false;
            checkBox10.Checked = false;
        }
        private void disableChceckboxes()
        {
            DataGridViewRow row = dataGridView2.CurrentRow;
            if (connection.myPrivileges.Insert.ToString() == "True" &&
                row.Cells[3].Value.ToString() == "False" && connection.myPrivileges.InsertIsGrantable.ToString() == "True")
            {
                    checkBox1.Enabled = true;
                    checkBox5.Enabled = true;
            }
            else
            {
                checkBox1.Enabled = false;
                checkBox5.Enabled = false;
            }
            if (connection.myPrivileges.Delete.ToString() == "True" &&
                row.Cells[5].Value.ToString() == "False" && connection.myPrivileges.DeleteIsGrantable.ToString() == "True")
            {
                checkBox2.Enabled = true;
                checkBox6.Enabled = true;
            }
            else
            {
                checkBox2.Enabled = false;
                checkBox6.Enabled = false;
            }
            if (connection.myPrivileges.Update.ToString() == "True" &&
                row.Cells[7].Value.ToString() == "False" && connection.myPrivileges.UpdateIsGrantable.ToString() == "True")
            {
                checkBox3.Enabled = true;
                checkBox7.Enabled = true;
            }
            else
            {
                checkBox3.Enabled = false;
                checkBox7.Enabled = false;
            }
            if (connection.myPrivileges.Select.ToString() == "True" &&
                row.Cells[1].Value.ToString() == "False" && connection.myPrivileges.SelectIsGrantable.ToString() == "True")
            {
                checkBox4.Enabled = true;
                checkBox8.Enabled = true;
            }
            else
            {
                checkBox4.Enabled = false;
                checkBox8.Enabled = false;
            }
            if(connection.myPrivileges.TakeOver.ToString() == "True" &&
                row.Cells[9].Value.ToString() == "False" && connection.myPrivileges.TakeOverIsGrantable.ToString() == "True")
            {
                checkBox9.Enabled = true;
                checkBox10.Enabled = true;
            }
            else
            {
                checkBox9.Enabled = false;
                checkBox10.Enabled = false;
            }
            uncheck();
        }
        //kliknięcie wybranej tabeli, powoduje pojwenie się uprawnień użytkowników
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            disableAllCheckboxes();
            uncheck();
            dataGridView2.Rows.Clear();
            dataGridView2.Refresh();
            if (e.RowIndex >= 0) 
            {
                chosenTable = dataGridView1.Rows[e.RowIndex].Cells["TableID"].Value.ToString();
                var nameTable = dataGridView1.Rows[e.RowIndex].Cells["TableID"].Value.ToString();
                var list2 = connection.GetTablePrivilegesAllUsers(nameTable);
                connection.ListGrantee = list2;
                foreach (var tabel in list2)
                {
                    if (tabel.UserName == connection.Login)
                        connection.myPrivileges = tabel;
                    dataGridView2.Rows.Add(tabel.UserName, tabel.Select, tabel.SelectIsGrantable, tabel.Insert, tabel.InsertIsGrantable, tabel.Delete, tabel.DeleteIsGrantable, tabel.Update, tabel.UpdateIsGrantable, tabel.TakeOver, tabel.TakeOverIsGrantable);
                }
                if(dataGridView2.RowCount > 0)
                dataGridView2.Rows[0].Selected = false;
            }
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if(dataGridView2.CurrentRow != null)
            disableChceckboxes();
            if (connection.myPrivileges.TakeOverIsGrantable.ToString() == "True" && dataGridView2.CurrentCell != null)
                button2.Enabled = true;
            else
                button2.Enabled = false;
        }

        private void takeBackPrivilege(string userName, string privilege, string tableName)//kto traci, co traci, gdzie traci
        {
            string connectionString = string.Format("Server={0}; Port={1}; database={2}; UID={3}; password={4};", connection.Server, connection.Port, connection.DatabaseName, connection.Login, connection.Password);
            MySqlConnection myConnection = new MySqlConnection(connectionString);
            MySqlCommand cmd1 = myConnection.CreateCommand();
            var usersPrivileges = connection.GetTablePrivilegesAllUsers(tableName);
            if(privilege == "SELECT")
            {
                if(connection.myPrivileges.SelectIsGrantable) //jeżeli możemy nadawać uprawnienia
                foreach (var user in usersPrivileges)//sprawdzamy uprawnienia każdego użytkownika
                {
                    if(user.Select && user.fromWho["Select"]==userName)//jeżeli użytkownik ma uprawnienie i ma je ode mnie
                    {
                        takeBackPrivilege(user.UserName, privilege, tableName);
                    }
                }
            }
            else if (privilege == "INSERT")
            {
                if (connection.myPrivileges.InsertIsGrantable) //jeżeli możemy nadawać uprawnienia
                    foreach (var user in usersPrivileges)//sprawdzamy uprawnienia każdego użytkownika
                    {
                        if (user.Insert && user.fromWho["Insert"] == userName)//jeżeli użytkownik ma uprawnienie i ma je ode mnie
                        {
                            takeBackPrivilege(user.UserName, privilege, tableName);
                        }
                    }
            }
            else if (privilege == "DELETE")
            {
                if (connection.myPrivileges.DeleteIsGrantable) //jeżeli możemy nadawać uprawnienia
                    foreach (var user in usersPrivileges)//sprawdzamy uprawnienia każdego użytkownika
                    {
                        if (user.Delete && user.fromWho["Delete"] == userName)//jeżeli użytkownik ma uprawnienie i ma je ode mnie
                        {
                            takeBackPrivilege(user.UserName, privilege, tableName);
                        }
                    }
            }
            else if (privilege == "UPDATE")
            {
                if (connection.myPrivileges.UpdateIsGrantable) //jeżeli możemy nadawać uprawnienia
                    foreach (var user in usersPrivileges)//sprawdzamy uprawnienia każdego użytkownika
                    {
                        if (user.Update && user.fromWho["Update"] == userName)//jeżeli użytkownik ma uprawnienie i ma je ode mnie
                        {
                            takeBackPrivilege(user.UserName, privilege, tableName);
                        }
                    }
            }
            else if(privilege =="TAKEOVER")
            {
                if (connection.myPrivileges.UpdateIsGrantable) //jeżeli możemy nadawać uprawnienia
                    foreach (var user in usersPrivileges)//sprawdzamy uprawnienia każdego użytkownika
                    {
                        if (user.Update && user.fromWho["Update"] == userName)//jeżeli użytkownik ma uprawnienie i ma je ode mnie
                        {
                            takeBackPrivilege(user.UserName, privilege, tableName);
                        }
                    }
                myConnection.Open();
                cmd1.CommandText = string.Format("DELETE FROM uprawnienia.user_privileges WHERE GRANTEE = \"{0}\" AND PRIVILEGE_TYPE = '{1}';",
                userName, tableName, privilege); //usuwamy swoje uprawnienie z każdej tabeli, ponieważ dotyczy to przejmowania, które jest globalne
                cmd1.ExecuteReader();
                myConnection.Close();
                return;
            }
            myConnection.Open();
            cmd1.CommandText = string.Format("DELETE FROM uprawnienia.user_privileges WHERE GRANTEE = \"{0}\" AND TABLE_NAME = '{1}' AND PRIVILEGE_TYPE = '{2}';",
            userName, tableName, privilege); //usuwamy swoje uprawnienie
            cmd1.ExecuteReader();
            myConnection.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
           /* string connectionString = string.Format("Server={0}; Port={1}; database={2}; UID={3}; password={4};", connection.Server, connection.Port, connection.DatabaseName, connection.Login, connection.Password);
            MySqlConnection myConnection = new MySqlConnection(connectionString);
            myConnection.Open();
            String userName = "'" + dataGridView2.CurrentCell.Value.ToString() + "'@'%'";
            String myUserName = "'" + connection.Login + "'@'%'";
            MySqlCommand cmd1 = myConnection.CreateCommand();
        //usuwanie własnych uprawnień
            cmd1.CommandText = string.Format("DELETE FROM uprawnienia.user_privileges WHERE GRANTEE = \"{0}\" AND TABLE_NAME = '{1}';",
                myUserName, dataGridView1.CurrentCell.Value.ToString());
            cmd1.ExecuteReader();
            myConnection.Close();
            //myConnection.Open();
            //pobieranie zabieranych uprawnien i nadanie ich sobie
            DataGridViewRow row = dataGridView2.CurrentRow;
            MySqlCommand cmd2 = myConnection.CreateCommand();
            for (int i = 1; i < 10; i+=2)
            {
               
                if (row.Cells[i].Value.ToString() == "True")
                {
                    myConnection.Open();
                    cmd1.CommandText = string.Format("INSERT INTO uprawnienia.user_privileges VALUES(\"{0}\", '{1}', '{2}', '{3}');", myUserName, dataGridView1.CurrentCell.Value.ToString(), dataGridView2.Columns[i].HeaderText.ToString(), row.Cells[i + 1].Value.ToString() == "False" ? "NO" : "YES"); //dataGridView2.Rows[-1].Cells[i].Value.ToString()
                    cmd1.ExecuteReader();
                    myConnection.Close();
                }

            }
            //usuwanie uprawnień użytkownikowi, od którego przejmujemy
            MySqlCommand cmd3 = connection.connection.CreateCommand();
            myConnection.Open();
            cmd1.CommandText = string.Format("DELETE FROM uprawnienia.user_privileges WHERE GRANTEE = \"{0}\" AND TABLE_NAME = '{1}'; ", userName, dataGridView1.CurrentCell.Value.ToString());
            cmd1.ExecuteReader();
            myConnection.Close();
            disableChceckboxes();*/
        }
    }
}
