# 🧠 Task Manager Backend — Azure Functions (.NET 8 Isolated)

This is a secure, serverless backend for a Task Manager app, powered by **Azure Functions**, **.NET 8 Isolated Worker**, and **Cosmos DB**. Built for modern, stateless APIs with JWT-based authentication.

---

## 🚀 Features

- ✅ **User Registration**
- 🔐 **Login with JWT authentication**
- 👤 **JWT-protected task endpoints**
- 📄 **CRUD Operations** for Tasks:
  - Create Task
  - Get All Tasks (per user)
  - Get Task by ID
  - Update Task (e.g. mark as complete or edit title/description)
  - Delete Task
- 🪪 **Authorization handled via JWT claims**
- 🗂 **Cosmos DB** integration for `Users` and `Tasks` containers
- 🧪 **Tested with Postman & curl**
- 📦 Uses `Newtonsoft.Json` for stable deserialization
- 🧼 Logout handled via frontend by removing JWT token (no server-side session)

---

## 🛠️ Technologies Used

- [.NET 8 Isolated Worker SDK](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- Azure Functions
- Azure Cosmos DB (NoSQL API)
- JWT Authentication with `System.IdentityModel.Tokens.Jwt`
- `Newtonsoft.Json` for request/response parsing

---

## 🔐 Environment Variables

These must be set for secure token generation and Cosmos DB access.

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

## 📬 Example JSON Request: Register/Login

```json
{
  "email": "may@example.com",
  "password": "password123"
}
```

---

## 🧾 Example JSON Request: Create Task

```json
{
  "title": "Finish README",
  "description": "Complete the project documentation",
  "userEmail": "may@example.com"
}
```

---

---

## ✅ Status

✅ All endpoints implemented  
✅ CosmosDB integration complete  
🔜 Frontend connection via Blazor (in progress)

---

## 📄 License

MIT License — © Mayowa Oladimeji
