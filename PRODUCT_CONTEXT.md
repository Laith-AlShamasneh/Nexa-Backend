# Product Context Document
# Education & Service Business Management Platform

## 1. Product Vision

We are building a modern SaaS platform that helps small and medium-sized education businesses and service centers manage their daily operations from one simple system.

The first target market is:
- Language institutes
- Training centers
- Educational academies

The long-term vision is to evolve into a general Business Operating System for service-based businesses such as:
- Education institutes
- Training centers
- Beauty centers
- Clinics
- Other appointment/service-based businesses

The product should not be designed as a simple "school management system".

The correct mindset:

We are building a flexible multi-tenant business management platform with vertical modules.

Education is our first vertical.

---

# 2. Business Problem

Many small and medium institutes in Jordan still manage their operations using:

- Excel files
- Paper records
- WhatsApp messages
- Manual payment tracking

This creates several business problems:

## Student/customer management problems:
- Customer information is scattered.
- Management cannot easily find customer history.
- No unified profile exists.

## Payment problems:
- Missed installments.
- Difficult debt tracking.
- Lack of financial visibility.
- Manual receipts.

## Attendance problems:
- Manual attendance sheets.
- No early warning for inactive students.
- No attendance analytics.

## Communication problems:
- Manual WhatsApp messages.
- No organized notifications.
- Poor follow-up.

## Management problems:
Owners cannot easily answer:

- How many active students do I have?
- How much money did we collect this month?
- Who has unpaid balances?
- Which courses are performing well?
- Which students may leave?

The product solves these operational problems.

---

# 3. Target Customer (Initial ICP)

Our first customers are:

Small and medium training institutes.

Typical profile:

- 100-500 students
- 5-20 instructors
- 1-5 administrative employees
- Owner is the main decision maker
- Currently using Excel and WhatsApp

Examples:

- English institutes
- IELTS centers
- Coding academies
- Professional training centers

---

# 4. Product Positioning

We do not sell "software".

We sell:

"Helping institutes move from Excel and manual processes into a simple digital operation system."

Main value proposition:

- Better student management
- Better payment collection
- Better communication
- Better business visibility

---

# 5. Product Strategy

The product strategy is:

## Core Platform + Vertical Modules

The system consists of:

## Core Platform

Reusable across industries:

- Multi-tenancy
- Authentication
- Users
- Roles and permissions
- Organizations
- Branches
- Customers
- Services
- Payments
- Notifications
- Reports


## Education Module

Specific for institutes:

- Courses
- Classes/Batches
- Enrollments
- Attendance
- Teachers assignment

---

# 6. Multi-Tenant Requirement

This is a SaaS product.

Multi-tenancy must be considered from day one.

Each organization/customer using the platform is a separate tenant.

Example:

Tenant 1:
ABC English Institute

Tenant 2:
XYZ Coding Academy


All business data must be isolated by:

OrganizationId


Every business-related database table should support tenant isolation.

Never design tables assuming a single organization.

---

# 7. Initial MVP Scope (Version 1)

The first version must focus on the minimum features that create business value.

## Module 1: Identity & Access Management

Features:

- Login
- Authentication
- Authorization
- Users
- Roles
- Permissions


Roles example:

- Owner
- Admin
- Accountant
- Teacher


---

# Module 2: Organization Management

Features:

- Organization profile
- Branches
- Settings


Even if V1 supports one branch, the database should support multiple branches.

---

# Module 3: Customer Management

Important architectural decision:

The database entity should be called Customer, not Student.

Reason:

The platform will support other industries later.

Education UI can display "Students".

Customer contains:

- Basic information
- Contact details
- Status
- Notes
- History


Future examples:

Education:
Customer = Student

Clinic:
Customer = Patient

Beauty:
Customer = Client


---

# Module 4: Service Management

A generic concept.

A business provides services.

Education:

Service = Course

Clinic:

Service = Treatment

Beauty:

Service = Session


Features:

- Create service
- Update service
- Price
- Duration
- Status


---

# Module 5: Enrollment / Subscription

Represents the relationship between customer and service.

Education:

Student enrolls in course.

Future:

Customer subscribes to package.


---

# Module 6: Payments (Hero Feature)

This is the most important module.

The business value is revenue visibility.

Features:

- Payment recording
- Payment history
- Payment plans
- Installments
- Outstanding balances
- Receipts


The owner should easily answer:

- Who owes money?
- How much?
- When is payment due?
- How much revenue did we collect?


---

# Module 7: Attendance

Education-specific but designed carefully.

Features:

- Attendance records
- Present/Absent/Late
- Attendance reports


Future possibilities:

Attendance can support other service businesses.

---

# Module 8: Communication

Initial version:

- Notification templates
- Notification history
- Manual WhatsApp links
- Email support


Future:

- WhatsApp Business API
- Automated reminders


---

# Module 9: Dashboard

Main user:

Owner


Dashboard should answer:

Business questions:

## Students:

- Total customers
- Active customers
- New registrations


## Finance:

- Revenue
- Outstanding payments
- Collection rate


## Operations:

- Attendance
- Active courses


---

# 8. Database Design Principles

Important rules:

## Rule 1:

Do not tightly couple the database to education.

Bad:

StudentId in Payments


Good:

CustomerId in Payments


---

## Rule 2:

Use generic naming for Core entities.

Examples:

Good:

Customers

Services

Payments


Avoid:

Students

Courses

TuitionPayments


inside core modules.

---

## Rule 3:

Design for future growth.

The database should support:

- Multiple tenants
- Multiple branches
- Multiple users
- Multiple service types

---

# 9. Future Roadmap

## Version 1.1

After first customer feedback:

- Import students from Excel
- Better reports
- Teacher portal
- WhatsApp integration
- Advanced notifications


---

## Version 2

Expand education capabilities:

- Parent portal
- Student portal
- Online payments
- Certificates
- Exams
- Academic tracking


---

## Version 3

Expand to other industries:

Examples:

## Clinics:

- Patients
- Appointments
- Medical records


## Beauty centers:

- Clients
- Booking
- Packages


---

# 10. Product Philosophy

Important:

Do not build a complicated ERP.

The target customer is small and medium businesses.

The product should be:

- Simple
- Fast
- Easy to learn
- Mobile friendly
- Affordable


The main competitor is not another software.

The main competitor is:

Excel + WhatsApp.

---

# 11. Development Mindset

When implementing features:

Always think:

1. Does this solve a real business problem?
2. Is this reusable in future verticals?
3. Does this break multi-tenancy?
4. Is the user experience simple?
5. Can a non-technical business owner understand it?


Do not optimize only for code quality.

Optimize for:

Business value + maintainability + scalability.

---

# Final Product Statement

We are building:

"A simple but powerful SaaS operating system that helps small and medium education businesses manage customers, services, payments, attendance, and communication while being architected to expand into other service industries in the future."
