using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Web.Configuration;
using Microsoft.Azure.Services.AppAuthentication;

namespace ServiceManagedIdentityTest
{
    public partial class Default : System.Web.UI.Page
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            using(SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = WebConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
                // DataSource != LocalDB means app is running in Azure with the SQLDB connection string you configured
                if (conn.DataSource != "(LocalDb)\\MSSQLLocalDB")
                    conn.AccessToken = (new AzureServiceTokenProvider()).GetAccessTokenAsync("https://database.windows.net/").Result;

                conn.Open();
                //Query to create the table if it doesn't exist
                string query = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServicePrincipalTest]') AND type in (N'U'))
                    BEGIN
	                    CREATE TABLE [dbo].[ServicePrincipalTest](
		                    [id] [int] IDENTITY(1,1) NOT NULL,
		                    [opened] [datetime] NOT NULL,
		                    [rand_int] [int] NOT NULL,
		                    [current_user] [varchar](100) NOT NULL,
	                     CONSTRAINT [PK_ServicePrincipalTest] PRIMARY KEY CLUSTERED 
	                    (
		                    [id] ASC
	                    )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
	                    ) ON [PRIMARY];

	                    ALTER TABLE [dbo].[ServicePrincipalTest] ADD  CONSTRAINT [DF_ServicePrincipalTest_opened]  DEFAULT (getdate()) FOR [opened];

	                    ALTER TABLE [dbo].[ServicePrincipalTest] ADD  CONSTRAINT [DF_ServicePrincipalTest_current_user]  DEFAULT (suser_sname()) FOR [current_user];

	                    CREATE NONCLUSTERED INDEX [IX_Coverage] ON [dbo].[ServicePrincipalTest] ([id] ASC) INCLUDE ( [opened], [rand_int], [current_user]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY];
                    END";

                SqlCommand comm = new SqlCommand(query, conn);

                await comm.ExecuteNonQueryAsync();

                //Insert a random number
                Random rand = new Random();

                query = $"INSERT INTO [dbo].[ServicePrincipalTest] ([rand_int]) VALUES ({rand.Next(1, 1000000)})";

                comm = new SqlCommand(query, conn);

                await comm.ExecuteNonQueryAsync();

                //Select everything and put it in the data table
                query = @"SELECT [id]
                              ,[opened]
                              ,[rand_int]
                              ,[current_user]
                          FROM [dbo].[ServicePrincipalTest]";

                comm = new SqlCommand(query, conn);

                DataTable data = new DataTable();
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(comm);

                sqlDataAdapter.Fill(data);

                //Attach the data table to the gridView
                gridView.DataSource = data;
                gridView.DataBind();
            }
        }
    }
}