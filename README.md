# Build Robust Software with Chaos Engineering Workshop: Infrastructure

This repository is part of the **Build Robust Software with Chaos Engineering Workshop** and demonstrates how to set up and run a **MySQL Flights Database** alongside **Toxiproxy** using Docker Compose. The `flights-db` image contains a MySQL database pre-loaded with flights-related data, and **Toxiproxy** allows you to simulate network conditions for testing fault tolerance.

## Project Structure

```
.
├── docker-compose.yml   # Docker Compose file to set up the environment
├── .env                 # Environment variables file for sensitive information
└── README.md            # Project documentation
```

## Prerequisites

Make sure you have the following installed on your system:

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [MySQL Client](https://dev.mysql.com/downloads/)

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

### Step 2: Create an `.env` File

Before running the Docker containers, create an `.env` file in the project root to inject sensitive information such as the MySQL root password.

```
touch .env
```

Add the following to the `.env` file:

```
MYSQL_ROOT_PASSWORD=my_secure_password
```

This file sets the root password for the MySQL database.

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

### Environment Variables

The `.env` file is used to inject sensitive configuration values. Below are the variables used in this project:

- **MYSQL_ROOT_PASSWORD**: The root password for MySQL.

### Stopping and Cleaning Up

To stop the running containers:

```
docker-compose down
```

To remove all containers, volumes, and networks created by Docker Compose:

```
docker-compose down --volumes --remove-orphans
```

## License

This project is licensed under the MIT License.

