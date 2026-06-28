# AutoPartsShop / Pardi Auto

AutoPartsShop is a desktop application built with C#, WPF, and .NET 8, designed as a management system for an auto parts shop. The project includes authentication, user roles, product management, orders, invoices, payments, and an administrative dashboard.

## Main Features

- user registration and login
- secure password storage using SHA-256 hashing
- role-based access system: Client, Employee, and Administrator
- interface adapted based on the authenticated user's role
- auto parts catalog with search, filtering, and sorting
- product management: add, edit, delete, and update stock
- order placement by clients
- viewing and finalizing personal orders
- order management by employees and administrators
- invoice generation for orders
- payment registration, update, and deletion
- synchronization of statuses between orders, invoices, and payments
- admin dashboard with statistics about users, products, orders, invoices, and revenue
- user role management by administrators
- user profile with contact details and delivery address
- audit/logging system for important application actions

## Technologies Used

- C#
- .NET 8
- WPF / XAML
- SQL Server LocalDB
- Microsoft.Data.SqlClient
- ADO.NET
- Git

## Database

The application uses a relational SQL Server database, with tables for:

- Users
- UserDetails
- Products
- Orders
- OrderProducts
- Invoices
- Payments
- Reports

The schema includes primary keys, foreign keys, stock and quantity constraints, and relationships between orders, products, invoices, and payments.

---


Through this project, I built a complete desktop application with real business workflows for an auto parts shop. I implemented authentication, role-based authorization, CRUD operations, database integration, validation, stock management, invoice generation, payments, and administrative reporting.

The project demonstrates practical experience with C#, WPF, SQL Server, relational database design, and organizing an application across multiple pages and models. It also includes important concepts such as parameterized queries, transactions for sensitive operations, data synchronization between entities, and separating functionality based on user roles.
