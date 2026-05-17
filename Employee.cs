using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Worker
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Employee()
        {
        }

        // Reads all employees from the Employee table
        public static List<Employee> GetAllEmployees(string connectionString)
        {
            var employees = new List<Employee>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT Id, Name FROM Employee", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new Employee
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return employees;
        }
    }
}
