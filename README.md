# SlotBook — Appointment Booking Web App

SlotBook is a clean and simple appointment booking web application built with **ASP.NET Core 8 MVC** and **MySQL**. It allows customers to discover local service providers (Clinics, Salons, and Tutors), view available 30-minute time slots, and schedule appointments easily.

---

## Features

- **Authentication & Roles**: Simple registration and login with cookie authentication (`Customer`, `Provider`, and `Admin` roles).
- **Provider Directory**: Browse service providers filtered by categories (Clinic, Salon, Tutor).
- **Interactive 30-Min Slot Booking**: Pick a date and time slot (between 09:00 AM and 05:00 PM) to book an appointment with real-time slot availability.
- **Double-Booking Prevention**: Ensures that no two customers can reserve the same time slot simultaneously.
- **Provider Dashboard**: Providers can manage services, set prices in Indian Rupee (₹), generate daily time slots, and view or complete appointments.
- **Admin Portal**: Platform admin can view overall metrics, registered providers, and all bookings.
- **Pure MVC**: Server-rendered Razor views styled with Bootstrap 5 and the **Inter** font family, paired with vanilla JavaScript. No React, Tailwind, or Node.js dependencies.

---

## Tech Stack

- **Backend**: C# / ASP.NET Core 8 (MVC)
- **Database**: MySQL 8.0 with Entity Framework Core (EF Core)
- **Frontend**: Razor Views (HTML5), CSS, Bootstrap 5, Vanilla JavaScript
- **Typography**: Inter (Google Fonts)

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL Server 8.0](https://dev.mysql.com/downloads/mysql/)

### Setup & Run

1. **Clone the repository**:
   ```bash
   git clone https://github.com/ananikets18/slotbook---A-Appointment-Booking-Web-app.git
   cd slotbook---A-Appointment-Booking-Web-app
   ```

2. **Configure Database Connection**:
   Open `src/AppointmentBooking.Web/appsettings.json` and set your MySQL credentials:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=AppointmentBookingDb;User=root;Password=root;CharSet=utf8mb4;"
     }
   }
   ```

3. **Run the Application**:
   - **Windows 1-Click**:
     - Double-click `start-mysql.bat` to start MySQL on port 3306.
     - Double-click `run-app.bat` to run the web application.
   - **Or via Command Line**:
     ```bash
     dotnet run --project src/AppointmentBooking.Web/AppointmentBooking.Web.csproj
     ```

4. **Open in Browser**:
   - Web Application: **http://localhost:5000**
   - API Documentation: **http://localhost:5000/swagger**

---

## Project Structure

```
AppointmentBooking/
├── src/
│   ├── AppointmentBooking.Core/            # Entities, enums, interfaces
│   ├── AppointmentBooking.Application/     # Business logic & services
│   ├── AppointmentBooking.Infrastructure/  # EF Core, MySQL DbContext, repositories & seeder
│   └── AppointmentBooking.Web/             # MVC controllers, Razor views & static assets
├── start-mysql.bat                         # Helper script to launch local MySQL
├── run-app.bat                             # Helper script to launch the web app
└── AppointmentBooking.sln                  # Visual Studio / .NET Solution
```

---

## Manual & API Testing (Postman / Browser)

- **Browser Testing**:
  - Open **`http://localhost:5000`** in your browser.
  - Test registration and login, browse categories, book 30-minute slots, and verify double-booking prevention.
  - Test provider workflow via the **Provider Dashboard** and platform management via the **Admin Portal**.

- **API & Postman Testing**:
  - Open **`http://localhost:5000/swagger`** or import endpoints into **Postman**:
    - `GET /api/slots/all?providerId={id}&date={yyyy-MM-dd}` — Fetch all slots for a provider.
    - `GET /api/slots/available?providerId={id}&date={yyyy-MM-dd}` — Fetch unbooked available slots.
    - `POST /api/bookings` — Create a new appointment booking.
    - `POST /api/bookings/{id}/cancel` — Cancel a booking and release the slot.
