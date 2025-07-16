# 🧠 Task Manager Backend — Azure Functions (.NET 8 Isolated)

This is a secure, serverless backend for a Task Manager app, powered by **Azure Functions**, **.NET 8 Isolated Worker**, and **Cosmos DB**. Built for modern, stateless APIs with JWT-based authentication.

---

## 🚀 Features

- ✅ **User Registration** with `username`, `email`, and `password`
- 🔐 **Login with JWT authentication**
- 👤 **JWT-protected task endpoints**
- 📄 **CRUD Operations** for Tasks:
  - Create Task (with `priority`, `tags`, `dueDate`)
  - Get All Tasks (per user)
  - Filter Tasks by:
    - `priority`: low / medium / high
    - `isCompleted`: true / false
    - `dueDate`: today / tomorrow
  - Update Task (any property incl. title, description, completion status, etc.)
  - Delete Task
- 🪪 **Authorization handled via JWT claims**
- 🗂 **Cosmos DB** integration for `Users` and `Tasks` containers
- 🧪 **Tested with Postman & curl**
- 📦 Uses `Newtonsoft.Json` for consistent JSON handling
- 🧼 Logout handled via frontend by removing JWT token (no server-side session)

---

## 🛠️ Technologies Used

- [.NET 8 Isolated Worker SDK](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- Azure Functions
- Azure Cosmos DB (NoSQL API)
- JWT Authentication (`System.IdentityModel.Tokens.Jwt`)
- `Newtonsoft.Json` for request/response parsing

---

## 🔐 Environment Variables

| Key                        | Description                              |
| -------------------------- | ---------------------------------------- |
| `JWT_SECRET`               | Secret key for signing JWTs              |
| `JWT_ISSUER`               | Issuer (e.g. `https://yourdomain`)       |
| `JWT_AUDIENCE`             | Audience (e.g. `https://yourclient`)     |
| `CosmosDBConnectionString` | Your Cosmos DB primary connection string |

---

## 📁 Folder Structure

```
azurebackend/
│
├── Models/
│   ├── UserModel.cs
│   └── TaskModel.cs
│
├── RegisterUser.cs
├── LoginUser.cs
├── CreateTask.cs
├── GetTasks.cs
├── GetTaskById.cs
├── UpdateTask.cs
├── DeleteTask.cs
│
├── Program.cs
└── azurebackend.csproj
```

---

## 📬 Example JSON Requests

### 🔐 Register/Login

```json
{
  "username": "may_dev",
  "email": "may@example.com",
  "password": "password123"
}
```

### 📄 Create Task

```json
{
  "title": "Finish README",
  "description": "Complete the project documentation",
  "priority": "high",
  "tags": ["work"],
  "dueDate": "2025-07-17T00:00:00",
  "userEmail": "may@example.com"
}
```

### 📥 Filter Tasks (query string)

- `/api/GetTaskById?priority=high`
- `/api/GetTaskById?dueDate=2025-07-16`
- `/api/GetTaskById?isCompleted=false`

---

## ✅ Status

✅ All endpoints implemented  
✅ Cosmos DB integration complete  
🔜 Frontend UI (Blazor) coming soon

---

## 📄 License

MIT License — © Mayowa Oladimeji
