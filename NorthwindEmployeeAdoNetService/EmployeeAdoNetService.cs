using System;
using System.Data;
using System.Data.SqlClient;
namespace NorthwindEmployeeAdoNetService;

/// <summary>
/// A service for interacting with the "Employees" table using ADO.NET.
/// </summary>
public sealed class EmployeeAdoNetService
{
    private readonly DbProviderFactory _dbFactory;
    private readonly string _connectionString;
    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeAdoNetService"/> class.
    /// </summary>
    /// <param name="dbFactory">The database provider factory used to create database connection and command instances.</param>
    /// <param name="connectionString">The connection string used to establish a database connection.</param>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="dbFactory"/> or <paramref name="connectionString"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty or contains only white-space characters.</exception>
    public EmployeeAdoNetService(DbProviderFactory dbFactory, string connectionString)
    {
        // Validate dbFactory
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory), "Database provider factory cannot be null.");

        // Validate connectionString
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be empty or contain only white-space characters.", nameof(connectionString));
        }

        _connectionString = connectionString; // Store the connection string
    }

    /// <summary>
    /// Retrieves a list of all employees from the Employees table of the database.
    /// </summary>
    /// <returns>A list of Employee objects representing the retrieved employees.</returns>
    public IList<Employee> GetEmployees()
    {
        var employees = new List<Employee>();

        using (var connection = _dbFactory.CreateConnection())
        {
            if (connection == null)
            {
                throw new InvalidOperationException("Can't connect to database");
            }

            connection.ConnectionString = _connectionString;
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT EmployeeID, FirstName, LastName, Title FROM Employees";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Retrieve the EmployeeID and construct the Employee object
                        var employee = new Employee(reader.GetInt64(reader.GetOrdinal("EmployeeID")))
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")) // Handle nullable Title
                        };

                        employees.Add(employee);
                    }
                }
            }
        }

        return employees;
    }

    /// <summary>
    /// Retrieves an employee with the specified employee ID.
    /// </summary>
    /// <param name="employeeId">The ID of the employee to retrieve.</param>
    /// <returns>The retrieved an <see cref="Employee"/> instance.</returns>
    /// <exception cref="EmployeeServiceException">Thrown if the employee is not found.</exception>
    public Employee GetEmployee(long employeeId)
    {
        using (var connection = _dbFactory.CreateConnection())
        {
            if (connection == null)
            {
                throw new InvalidOperationException("Can't connect to database");
            }

            connection.ConnectionString = _connectionString;
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT  EmployeeID,FirstName,LastName,Title FROM Employees Where EmployeeID={employeeId}";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return new Employee(employeeId)
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")) // Handle nullable Title
                        };
                    }
                }
            }
        }
        throw new EmployeeServiceException($"Employee with ID {employeeId} not found.");
    }
    /// <summary>
    /// Adds a new employee to Employee table of the database.
    /// </summary>
    /// <param name="employee">The  <see cref="Employee"/> object containing the employee's information.</param>
    /// <returns>The ID of the newly added employee.</returns>
    /// <exception cref="EmployeeServiceException">Thrown when an error occurs while adding the employee.</exception>
    public long AddEmployee(Employee employee)
    {
        if (employee == null)
        {
            throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");
        }

        using (var connection = _dbFactory.CreateConnection())
        {
            if (connection == null)
            {
                throw new InvalidOperationException("Can't connect to database");
            }

            connection.ConnectionString = _connectionString;
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                INSERT INTO Employees (FirstName, LastName, Title)
                VALUES (@FirstName, @LastName, @Title);
                SELECT last_insert_rowid();"; // Gets the ID of the newly inserted employee

                // Add parameters to prevent SQL injection
                var firstNameParam = command.CreateParameter();
                firstNameParam.ParameterName = "@FirstName";
                firstNameParam.Value = employee.FirstName;
                command.Parameters.Add(firstNameParam);

                var lastNameParam = command.CreateParameter();
                lastNameParam.ParameterName = "@LastName";
                lastNameParam.Value = employee.LastName;
                command.Parameters.Add(lastNameParam);

                var titleParam = command.CreateParameter();
                titleParam.ParameterName = "@Title";
                titleParam.Value = employee.Title ?? (object)DBNull.Value; // Handle nullable Title
                command.Parameters.Add(titleParam);

                try
                {
                    // Execute the command and get the last inserted ID
                    var newId = (long)command.ExecuteScalar();
                    return newId; // Return the ID of the newly added employee
                }
                catch (Exception ex)
                {
                    throw new EmployeeServiceException("An error occurred while adding the employee.", ex);
                }
            }
        }
    }

    /// <summary>
    /// Removes an employee from the the Employee table of the database based on the provided employee ID.
    /// </summary>
    /// <param name="employeeId">The ID of the employee to remove.</param>
    /// <exception cref="EmployeeServiceException"> Thrown when an error occurs while attempting to remove the employee.</exception>
    public void RemoveEmployee(long employeeId)
    {
        using (var connection = _dbFactory.CreateConnection())
        {
            if (connection == null)
            {
                throw new InvalidOperationException("Cannot connect to the database.");
            }

            connection.ConnectionString = _connectionString;
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";

                // Add parameter to prevent SQL injection
                var employeeIdParam = command.CreateParameter();
                employeeIdParam.ParameterName = "@EmployeeID";
                employeeIdParam.Value = employeeId;
                command.Parameters.Add(employeeIdParam);

                try
                {
                    command.ExecuteNonQuery();
                    // No exception is thrown for a non-existent employee
                }
                catch (Exception ex)
                {
                    // Consider logging the exception here for diagnostics
                    throw new EmployeeServiceException("An error occurred while removing the employee.", ex);
                }
            }
        }
    }


    /// <summary>
    /// Updates an employee record in the Employee table of the database.
    /// </summary>
    /// <param name="employee">The employee object containing updated information.</param>
    /// <exception cref="EmployeeServiceException">Thrown when there is an issue updating the employee record.</exception>
    public void UpdateEmployee(Employee employee)
    {
        if (employee == null)
        {
            throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");
        }

        using (var connection = _dbFactory.CreateConnection())
        {
            if (connection == null)
            {
                throw new InvalidOperationException("Failed to create a database connection.");
            }

            connection.ConnectionString = _connectionString;
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Employees SET Name = @Name, Position = @Position, Salary = @Salary WHERE EmployeeID = @EmployeeID";

                command.Parameters.Add(CreateParameter("@EmployeeID", employee.Id));
                command.Parameters.Add(CreateParameter("@Name", employee.FirstName));
                command.Parameters.Add(CreateParameter("@Position", employee.LastName));
                if(employee.Title != null)
                command.Parameters.Add(CreateParameter("@Salary", employee.Title));

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new EmployeeServiceException($"No employee record was updated. Check if the employee exists.");
                }
            }
        }
    }


    // Helper method to create parameters
    private IDbDataParameter CreateParameter(string name, object value)
    {
        var parameter = _dbFactory.CreateParameter();
         if(parameter.Value != null)
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value; // Handle null values
        return parameter;
    }

}