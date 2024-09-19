# Build Robust Software with Chaos Engineering Workshop: Infrastructure

This repository is part of the **Build Robust Software with Chaos Engineering Workshop** and demonstrates how to set up and run a **MySQL Flights Database** alongside **Toxiproxy** using Docker Compose. The `flights-db` image contains a MySQL database pre-loaded with flights-related data, and **Toxiproxy** allows you to simulate network conditions for testing fault tolerance.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Services](#services)
3. [Setup and Usage](#setup-and-usage)
   1. [Step 1: Clone the Repository](#step-1-clone-the-repository)
   2. [Step 2: Create an `.env` File](#step-2-create-an-env-file-optional)
   3. [Step 3: Update and Run Docker Compose](#step-3-update-and-run-docker-compose)
   4. [Step 4: Add a Proxy in Toxiproxy](#step-4-add-a-proxy-in-toxiproxy)
   5. [Step 5: Verify the Proxy](#step-5-verify-the-proxy)
   6. [Step 6: Test the Connection to MySQL via Toxiproxy](#step-6-test-the-connection-to-mysql-via-toxiproxy)
4. [Common Docker Commands](#common-docker-commands-for-development-and-debugging)
5. [Advanced Configuration for Local Development](#advanced-configuration-for-local-development)
6. [Environment Variables](#environment-variables)
7. [Inducing Network Latency with Toxiproxy](#inducing-network-latency-with-toxiproxy)
8. [Cleanup](#cleanup)
9. [Troubleshooting](#troubleshooting)
10. [Contributing](#contributing)
11. [License](#license)

## Prerequisites

Make sure you have the following installed on your system:

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [MySQL Client](https://dev.mysql.com/downloads/)
- [.NET Core SDK](https://dotnet.microsoft.com/download)

## Services

This project includes the following services:

### 1. Flights Database (MySQL)

- **Image**: [`joaoasrosa/flights-db:latest`](https://hub.docker.com/repository/docker/joaoasrosa/flights-db/general)
- **Port**: Exposes **MySQL** on port `3306`
- **Database Name**: `flights`

### 2. Toxiproxy

- **Image**: [`toxiproxy:2.9.0`](https://github.com/Shopify/toxiproxy/pkgs/container/toxiproxy)
- **Port 8474**: Exposes **Toxiproxy API** on port `8474`
- **Port 3307**: Exposes a MySQL proxy on **port 3307**, forwarding traffic to **flights-db** on port **3306**.

Toxiproxy is used to simulate network conditions such as latency, packet loss, and more, to test the fault tolerance of applications interacting with the database.

## Setup and Usage

### Step 1: Clone the Repository

```
git clone https://github.com/joaoasrosa/build-robust-software-with-chaos-engineering-workshop.git
cd build-robust-software-with-chaos-engineering-workshop
```

### Step 2: Create an `.env` File (Optional)

Before running the Docker containers, you can optionally create an `.env` file in the project root to inject sensitive information such as the MySQL root password for Docker Compose. Here's a template you can use:

```
touch .env
```

Example contents:

```
MYSQL_ROOT_PASSWORD=my_secure_password
MYSQL_USER=my_user
MYSQL_PASSWORD=my_user_password
```

> **Note**: If you do not create an `.env` file, Docker Compose will use default environment values defined in the `docker-compose.yml`.

### Step 3: Update and Run Docker Compose

Use the following command to start the services defined in the `docker-compose.yml` file:

```
docker-compose up -d
```

This will:
- Start the **flights-db** service and expose it on port `3306`.
- Start the **toxiproxy** service and expose it on ports `8474` (API) and `3307` (MySQL proxy).

### Step 4: Add a Proxy in Toxiproxy

To enable proxying of MySQL traffic through **Toxiproxy** on **port 3307**, use the **Toxiproxy CLI**:

```
toxiproxy-cli create --listen 0.0.0.0:3307 --upstream flights-db:3306 mysql_proxy
```

Explanation:
- **`--listen 0.0.0.0:3307`**: Tells Toxiproxy to listen on **port 3307** on all network interfaces.
- **`--upstream flights-db:3306`**: Forwards traffic from port **3307** to the **flights-db** container on **port 3306**.
- **`mysql_proxy`**: The name of the proxy.

### Step 5: Verify the Proxy

To list active proxies and ensure the `mysql_proxy` was created successfully:

```
toxiproxy-cli list
```

### Step 6: Test the Connection to MySQL via Toxiproxy

Once the proxy is created, test the MySQL connection through **Toxiproxy** by running:

```
mysql -h 127.0.0.1 -P 3307 -u root -p
```

Enter the MySQL root password when prompted. If successful, this confirms that traffic is being correctly forwarded through Toxiproxy to MySQL.

## Common Docker Commands for Development and Debugging

- **Stop all containers**: 
  ```
  docker-compose down
  ```

- **Check running containers**:
  ```
  docker ps
  ```

- **Restart a specific service**:
  ```
  docker-compose restart <service_name>
  ```

- **Check logs of a specific service**:
  ```
  docker-compose logs -f <service_name>
  ```

> Replace `<service_name>` with either `flights-db` or `toxiproxy` depending on which logs you want to check.

## Advanced Configuration for Local Development

If you are developing locally and want to store sensitive data such as MySQL credentials securely, you can use **User Secrets**. Follow the steps below:

1. **Initialize User Secrets** in the project folder:
   
   ```
   dotnet user-secrets init
   ```

2. **Set the database username and password** using User Secrets:

   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection:DB_USER" "my_secure_user"
   dotnet user-secrets set "ConnectionStrings:DefaultConnection:DB_PASSWORD" "my_secure_password"
   ```

> **Note**: Replace `"my_secure_user"` and `"my_secure_password"` with the actual credentials.

## Environment Variables

You can configure the following environment variables to control MySQL behavior:

- `MYSQL_ROOT_PASSWORD`: Sets the root password for MySQL.
- `MYSQL_USER`: Optional user to create besides the root user.
- `MYSQL_PASSWORD`: The password for the optional user.

These can be set directly in the `.env` file or by passing them during Docker Compose execution.

## Inducing Network Latency with Toxiproxy

Once you have set up the MySQL proxy, you can simulate network issues such as high latency or dropped packets. For example, to simulate a 1000ms latency on MySQL traffic:

```
toxiproxy-cli toxic add mysql_proxy --type latency --latency 1000
```

This adds 1000ms of latency to all traffic passing through the MySQL proxy.

## Cleanup

To stop and remove the Docker containers and networks created for this project, run:

```
docker-compose down
```

This will stop and remove all services defined in the `docker-compose.yml`, including the MySQL and Toxiproxy containers.

## Troubleshooting

### Port 3306 or 3307 Already in Use

If you encounter errors indicating that the MySQL port (3306) or Toxiproxy port (3307) is in use, another service may already be using these ports. You can either stop those services or modify the `docker-compose.yml` file to use different ports.

### MySQL Connection Fails

If you have trouble connecting to the MySQL database, verify that:

1. The MySQL container is running: 
   ```
   docker-compose ps
   ```

2. You're using the correct port: `3306` for direct connection, `3307` for Toxiproxy.

3. Your `.env` file contains the correct MySQL credentials.

4. The root password set in the `.env` file matches the password you use to connect.

## Contributing

We welcome contributions to improve this project. Please open an issue or submit a pull request if you have suggestions or improvements.

## License

This project is licensed under the [MIT License](LICENSE).
