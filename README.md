# Flights Database and Toxiproxy Demo

This repository demonstrates how to set up and run a **MySQL Flights Database** alongside **Toxiproxy** using Docker Compose. The `flights-db` image contains a MySQL database pre-loaded with flights-related data, and **Toxiproxy** allows you to simulate network conditions for testing fault tolerance.

## Project Structure

```bash
.
├── docker-compose.yml   # Docker Compose file to set up the environment
├── .env                 # Environment variables file for sensitive information
└── README.md            # Project documentation
```

## Prerequisites

Make sure you have the following installed on your system:

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)

## Services

This project includes the following services:

### 1. Flights Database (MySQL)

- **Image**: [`joaoasrosa/flights-db:latest`](https://hub.docker.com/repository/docker/joaoasrosa/flights-db/general)
- **Port**: Exposes **MySQL** on port `3306`
- **Database Name**: `flights`

### 2. Toxiproxy

- **Image**: [`toxiproxy:2.9.0`](https://github.com/Shopify/toxiproxy/pkgs/container/toxiproxy)
- **Port**: Exposes **Toxiproxy API** on port `8474`

Toxiproxy is used to simulate network conditions such as latency, packet loss, and more, to test the fault tolerance of applications interacting with the database.

## Setup and Usage

### Step 1: Clone the Repository

```bash
git clone https://github.com/your-username/your-repo-name.git
cd your-repo-name
```

### Step 2: Create an `.env` File

Before running the Docker containers, create an `.env` file in the project root to inject sensitive information such as the MySQL root password.

```bash
touch .env
```

Add the following to the `.env` file:

```bash
MYSQL_ROOT_PASSWORD=my_secure_password
```

This file sets the root password for the MySQL database.

### Step 3: Run Docker Compose

Use the following command to bring up the containers:

```bash
docker-compose up -d
```

This will:
- Start the **flights-db** service and expose it on port `3306`.
- Start the **toxiproxy** service and expose it on port `8474`.

### Step 4: Verify the Services

1. **Check running containers**:
   
   ```bash
   docker ps
   ```

   You should see both `flights-db` and `toxiproxy` running.

2. **Access the MySQL database**:
   You can access the MySQL database using any MySQL client (such as the MySQL CLI or Workbench). Example command:

   ```bash
   mysql -h 127.0.0.1 -P 3306 -u root -p
   ```

   Enter the password you defined in the `.env` file (`my_secure_password`).

3. **Access Toxiproxy**:
   You can interact with Toxiproxy via its REST API at `http://localhost:8474`. For more details on how to use Toxiproxy, refer to the [Toxiproxy Documentation](https://github.com/Shopify/toxiproxy).

## Toxiproxy Usage Example

Here’s a quick example of how to create a proxy for the **MySQL** service using Toxiproxy to simulate network conditions.

1. Create a MySQL proxy using Toxiproxy's API:

   ```bash
   curl -XPOST http://localhost:8474/proxies -d '{
     "name": "mysql_proxy",
     "listen": "0.0.0.0:3307",
     "upstream": "flights-db:3306"
   }'
   ```

   This will create a proxy on port `3307` that forwards traffic to the `flights-db` MySQL service on port `3306`.

2. Add latency to simulate slow network conditions:

   ```bash
   curl -XPOST http://localhost:8474/proxies/mysql_proxy/toxics -d '{
     "name": "mysql_latency",
     "type": "latency",
     "attributes": {
       "latency": 2000
     }
   }'
   ```

   This will add 2 seconds of latency to all connections through the proxy.

3. Test your application's connection to MySQL through the proxy (`localhost:3307`) and observe the simulated latency.

## Environment Variables

The `.env` file is used to inject sensitive configuration values. Below are the variables used in this project:

- **MYSQL_ROOT_PASSWORD**: The root password for MySQL.

## Stopping and Cleaning Up

To stop the running containers:

```bash
docker-compose down
```

To remove all containers, volumes, and networks created by Docker Compose:

```bash
docker-compose down --volumes --remove-orphans
```

## License

This project is licensed under the MIT License.