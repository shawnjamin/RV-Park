namespace RVPark.Models;

// Access levels for Users. In ascending order from lowest to highest privileges
// Manager and Admin have the same level of access, they are only separate for database purposes
public enum AccessLevel
{
    Customer,
    Employee,
    Manager,
    Admin
}
