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
                command.CommandText = $"SELECT EmployeeID, FirstName, LastName, Title, TitleOfCourtesy, BirthDate, HireDate, Address, City, Region, PostalCode, Country, HomePhone, Extension, Notes, ReportsTo, PhotoPath FROM Employees WHERE EmployeeID = {employeeId}";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return new Employee(employeeId)
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                            TitleOfCourtesy = reader.IsDBNull(reader.GetOrdinal("TitleOfCourtesy")) ? null : reader.GetString(reader.GetOrdinal("TitleOfCourtesy")),
                            BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("BirthDate")),
                            HireDate = reader.IsDBNull(reader.GetOrdinal("HireDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("HireDate")),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
                            Region = reader.IsDBNull(reader.GetOrdinal("Region")) ? null : reader.GetString(reader.GetOrdinal("Region")),
                            PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? null : reader.GetString(reader.GetOrdinal("PostalCode")),
                            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
                            HomePhone = reader.IsDBNull(reader.GetOrdinal("HomePhone")) ? null : reader.GetString(reader.GetOrdinal("HomePhone")),
                            Extension = reader.IsDBNull(reader.GetOrdinal("Extension")) ? null : reader.GetString(reader.GetOrdinal("Extension")),
                            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                            ReportsTo = reader.IsDBNull(reader.GetOrdinal("ReportsTo")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ReportsTo")),
                            PhotoPath = reader.IsDBNull(reader.GetOrdinal("PhotoPath")) ? null : reader.GetString(reader.GetOrdinal("PhotoPath"))
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
                SELECT last_insert_rowid();";

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
                titleParam.Value = employee.Title ?? (object)DBNull.Value;
                command.Parameters.Add(titleParam);

                try
                {
                    var newId = (long)command.ExecuteScalar();
                    return newId;
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

                var employeeIdParam = command.CreateParameter();
                employeeIdParam.ParameterName = "@EmployeeID";
                employeeIdParam.Value = employeeId;
                command.Parameters.Add(employeeIdParam);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
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
    public async Task UpdateEmployee(Employee employee)
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
                command.CommandText = @"
                UPDATE Employees 
                SET 
                    FirstName = @FirstName, 
                    LastName = @LastName, 
                    Title = @Title, 
                    TitleOfCourtesy = @TitleOfCourtesy, 
                    BirthDate = @BirthDate, 
                    HireDate = @HireDate, 
                    Address = @Address, 
                    City = @City, 
                    Region = @Region, 
                    PostalCode = @PostalCode, 
                    Country = @Country, 
                    HomePhone = @HomePhone, 
                    Extension = @Extension, 
                    Notes = @Notes, 
                    ReportsTo = @ReportsTo, 
                    PhotoPath = @PhotoPath
                WHERE EmployeeID = @EmployeeID";

                
                var employeeIdParam = command.CreateParameter();
                employeeIdParam.ParameterName = "@EmployeeID";
                employeeIdParam.Value = employee.Id;
                command.Parameters.Add(employeeIdParam);

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
                titleParam.Value = employee.Title ?? ""; 
                command.Parameters.Add(titleParam);

                var titleOfCourtesyParam = command.CreateParameter();
                titleOfCourtesyParam.ParameterName = "@TitleOfCourtesy";
                titleOfCourtesyParam.Value = employee.TitleOfCourtesy ?? (object)DBNull.Value;
                command.Parameters.Add(titleOfCourtesyParam);

                var birthDateParam = command.CreateParameter();
                birthDateParam.ParameterName = "@BirthDate";
                birthDateParam.Value = employee.BirthDate ?? (object)DBNull.Value;
                command.Parameters.Add(birthDateParam);

                var hireDateParam = command.CreateParameter();
                hireDateParam.ParameterName = "@HireDate";
                hireDateParam.Value = employee.HireDate ?? (object)DBNull.Value;
                command.Parameters.Add(hireDateParam);

                var addressParam = command.CreateParameter();
                addressParam.ParameterName = "@Address";
                addressParam.Value = employee.Address ?? (object)DBNull.Value;
                command.Parameters.Add(addressParam);

                var cityParam = command.CreateParameter();
                cityParam.ParameterName = "@City";
                cityParam.Value = employee.City ?? (object)DBNull.Value;
                command.Parameters.Add(cityParam);

                var regionParam = command.CreateParameter();
                regionParam.ParameterName = "@Region";
                regionParam.Value = employee.Region ?? (object)DBNull.Value;
                command.Parameters.Add(regionParam);

                var postalCodeParam = command.CreateParameter();
                postalCodeParam.ParameterName = "@PostalCode";
                postalCodeParam.Value = employee.PostalCode ?? (object)DBNull.Value;
                command.Parameters.Add(postalCodeParam);

                var countryParam = command.CreateParameter();
                countryParam.ParameterName = "@Country";
                countryParam.Value = employee.Country ?? (object)DBNull.Value;
                command.Parameters.Add(countryParam);

                var homePhoneParam = command.CreateParameter();
                homePhoneParam.ParameterName = "@HomePhone";
                homePhoneParam.Value = employee.HomePhone ?? (object)DBNull.Value;
                command.Parameters.Add(homePhoneParam);

                var extensionParam = command.CreateParameter();
                extensionParam.ParameterName = "@Extension";
                extensionParam.Value = employee.Extension ?? (object)DBNull.Value;
                command.Parameters.Add(extensionParam);

                var notesParam = command.CreateParameter();
                notesParam.ParameterName = "@Notes";
                notesParam.Value = employee.Notes ?? (object)DBNull.Value;
                command.Parameters.Add(notesParam);

                var reportsToParam = command.CreateParameter();
                reportsToParam.ParameterName = "@ReportsTo";
                reportsToParam.Value = employee.ReportsTo ?? (object)DBNull.Value;
                command.Parameters.Add(reportsToParam);

                var photoPathParam = command.CreateParameter();
                photoPathParam.ParameterName = "@PhotoPath";
                photoPathParam.Value = employee.PhotoPath ?? (object)DBNull.Value;
                command.Parameters.Add(photoPathParam);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new EmployeeServiceException("Employee is not updated.");
                }
            }
        }
    }




    private IDbDataParameter CreateParameter(string name, object value)
    {
        var parameter = _dbFactory.CreateParameter();
         if(parameter.Value != null)
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }

}