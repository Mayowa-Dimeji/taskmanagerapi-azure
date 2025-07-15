# AzureBackend – Serverless Task Manager API

This is a serverless backend built with **.NET isolated worker Azure Functions**, using **Cosmos DB** as the database and **JWT authentication** for secure login. It powers a task management app with scalable, cloud-native architecture.

## 🚀 Features

- ✅ User Registration with validation
- ✅ Secure Login using **JWT tokens**
- ✅ Cosmos DB integration with auto-generated user IDs
- ✅ Azure Functions (.NET 8 isolated worker model)
- ✅ Ready to extend with protected endpoints (e.g., task management)

---

## 📁 Project Structure

```
azurebackend/
├── Models/
│   └── UserModel.cs
├── RegisterUser.cs
├── LoginUser.cs
├── Program.cs
└── azurebackend.csproj
```

---

## 🔐 Authentication

### ✅ JWT Tokens

- JWT tokens are generated using the user ID and email.
- The token is signed using a secret key from environment variables.

### Required Environment Variables

Add these variables to your local `.env` file or Azure configuration:

| Variable       | Description                        |
| -------------- | ---------------------------------- |
| `JWT_SECRET`   | A strong secret for signing tokens |
| `JWT_ISSUER`   | Your API's domain or app name      |
| `JWT_AUDIENCE` | Intended audience for the token    |

---

## 🗃 Database

- Uses **Azure Cosmos DB (SQL API)**.
- A container named `Users` stores registered users.
- Each user document includes:
  - `id`: Auto-generated GUID
  - `email`: Unique user email
  - `password`: Stored in plain text (🛑 _should be hashed for production_)

---

## 📬 API Endpoints

### Register User

**POST** `/api/RegisterUser`

**Body:**

```json
{
  "email": "user@example.com",
  "password": "securepassword"
}
```

**Response:** `201 Created` or `400 Bad Request`

---

### Login User

**POST** `/api/LoginUser`

**Body:**

```json
{
  "email": "user@example.com",
  "password": "securepassword"
}
```

**Response:** `200 OK`

```json
{
  "token": "your.jwt.token"
}
```

---

## 🛠 How to Run Locally

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the Azure Function app
func start
```

Make sure your `local.settings.json` or environment variables are set up for Cosmos DB and JWT.

---

## 🔧 Tech Stack

- **Azure Functions** (Isolated Worker, .NET 8)
- **Azure Cosmos DB** (SQL API)
- **JWT Authentication**
- **Newtonsoft.Json** for reliable serialization
- **C#** (.NET 8)

---

## ✅ Future Improvements

- 🔒 Add password hashing with `PasswordHasher`
- 🧠 Add user role claims
- 📌 Add protected endpoints for managing tasks (CRUD)
- 🧪 Add unit tests and integration tests
- ☁️ Deploy using GitHub Actions or Azure Pipelines

---

## 💬 Author

**Mayowa Oladimeji**  
Built with 💙 using .NET and Azure  
Feel free to contribute or fork the repo!

---

## 📜 License

MIT License
