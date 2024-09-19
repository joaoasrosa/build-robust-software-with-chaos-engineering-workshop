# Build Robust Software with Chaos Engineering Workshop: Infrastructure

This repository is part of the **Build Robust Software with Chaos Engineering Workshop** and demonstrates how to set up and run a **MySQL Flights Database** alongside **Toxiproxy** using Docker Compose. The `flights-db` image contains a MySQL database pre-loaded with flights-related data, and **Toxiproxy** allows you to simulate network conditions for testing fault tolerance.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Services](#services)
3. [Database Schema](#database-schema)
4. [Setup and Usage](#setup-and-usage)
   1. [Step 1: Clone the Repository](#step-1-clone-the-repository)
   2. [Step 2: Create an `.env` File](#step-2-create-an-env-file-optional)
   3. [Step 3: Update and Run Docker Compose](#step-3-update-and-run-docker-compose)
   4. [Step 4: Add a Proxy in Toxiproxy](#step-4-add-a-proxy-in-toxiproxy)
   5. [Step 5: Simulating Timeout with Latency and Jitter (exercise2)](#step-5-simulating-timeout-with-latency-and-jitter-exercise2)
   6. [Step 6: Simulating Timeouts with Retry Mechanism (exercise3)](#step-6-simulating-timeouts-with-retry-mechanism-exercise3)
   7. [Step 7: Simulating Failures with Circuit Breaker (exercise4)](#step-7-simulating-failures-with-circuit-breaker-exercise4)
   8. [Step 8: Testing the Health Endpoint (exercise5)](#step-8-testing-the-health-endpoint-exercise5)
   9. [Step 9: Verify the Proxy](#step-9-verify-the-proxy)
   10. [Step 10: Test the Connection to MySQL via Toxiproxy](#step-10-test-the-connection-to-mysql-via-toxiproxy)
5. [Common Docker Commands](#common-docker-commands-for-development-and-debugging)
6. [Advanced Configuration for Local Development](#advanced-configuration-for-local-development)
7. [Environment Variables](#environment-variables)
8. [Inducing Network Latency with Toxiproxy](#inducing-network-latency-with-toxiproxy)
9. [Cleanup](#cleanup)
10. [Troubleshooting](#troubleshooting)
11. [Contributing](#contributing)
12. [License](#license)

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

## Database Schema

Here is a representation of the database schema for the project, including relationships between tables like `airlines`, `airports`, `countries`, `locales`, and `routes`.

```mermaid
erDiagram
    AIRLINES {
        int alid PK "Primary key (Airline ID)"
        varchar name "Airline name"
        varchar iata "IATA code"
        varchar icao "ICAO code"
        varchar callsign "Airline callsign"
        varchar country "Country name"
        int uid "User ID (optional)"
        varchar alias "Airline alias"
        char mode "Mode (F for full, N for inactive)"
        char active "Active status (Y/N)"
    }

    AIRPORTS {
        int apid PK "Primary key (Airport ID)"
        varchar name "Airport name"
        varchar city "City name"
        varchar country "Country name"
        varchar iata "IATA code"
        varchar icao "ICAO code"
        double x "Longitude"
        double y "Latitude"
        int elevation "Elevation"
        int uid "User ID (optional)"
        float timezone "Timezone"
        char dst "Daylight saving time"
        varchar tz_id "Timezone ID"
        varchar type "Airport type"
        varchar source "Source of the data"
        varchar country_code FK "Foreign key to countries.code"
    }

    COUNTRIES {
        varchar code PK "Country code (ISO)"
        varchar name "Country name"
        varchar oa_code "Other code (optional)"
        char dst "Daylight saving time"
    }

    LOCALES {
        varchar locale PK "Locale code"
        varchar name "Locale name"
    }

    ROUTES {
        int rid PK "Primary key (Route ID)"
        varchar airline "Airline code"
        int alid FK "Foreign key to airlines.alid"
        varchar src_ap "Source airport code"
        int src_apid FK "Foreign key to airports.apid"
        varchar dst_ap "Destination airport code"
        int dst_apid FK "Foreign key to airports.apid"
        varchar codeshare "Codeshare information"
        tinyint stops "Number of stops"
        varchar equipment "Equipment used on the route"
    }

    AIRLINES ||--o{ ROUTES : "uses"
    AIRPORTS ||--o{ ROUTES : "serves"
    COUNTRIES ||--o{ AIRPORTS : "located in"
  ```


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

### Step 5: Simulating Timeout with Latency and Jitter (exercise2)

If you're working on the `exercise2` branch, please make sure you have switched to the correct branch:

```
git checkout exercise2
```

In this branch, we introduce a **stability pattern**: a timeout that simulates network instability using **Toxiproxy**.

To simulate latency with jitter, you can add a "toxic" (fault) to the `mysql_proxy` created earlier. Use the following **Toxiproxy CLI** command to simulate network instability by adding both latency and jitter:

```
toxiproxy-cli toxic add -t latency --tox 1 -a "latency=100" -a "jitter=900" mysql_proxy
```

Explanation:
- **`-t latency`**: Specifies that the toxic being added is a latency fault.
- **`--tox 1`**: Sets the toxicity level to 100% (meaning all traffic is affected).
- **`-a "latency=100"`**: Adds a base latency of 100ms to all traffic.
- **`-a "jitter=900"`**: Adds random variability (jitter) of up to 900ms to simulate real-world network conditions where latency fluctuates unpredictably.

> **Result**: This setup simulates a scenario where the MySQL connection can experience random latencies ranging from 100ms to 1000ms (100 + 900), leading to potential timeouts in your application.

### Step 6: Simulating Timeouts with Retry Mechanism (exercise3)

If you're working on the `exercise3` branch, ensure that you switch to it:

```
git checkout exercise3
```

In this branch, a **retry mechanism** is added to handle database calls in case of instability. This branch helps test how the application responds to database timeouts by retrying failed operations.

You can simulate database timeouts using the following **Toxiproxy CLI** command:

```
toxiproxy-cli toxic add -t timeout --tox 0.8 -a "timeout=500" mysql_proxy
```

Explanation:
- **`-t timeout`**: Adds a timeout toxic to the proxy.
- **`--tox 0.8`**: Specifies that 80% of traffic will be affected by the timeout.
- **`-a "timeout=500"`**: Adds a timeout delay of 500ms to the affected traffic.

> **Result**: This toxic simulates a scenario where 80% of the MySQL traffic will timeout after 500ms, triggering the retry mechanism in the application.

### Step 7: Simulating Failures with Circuit Breaker (exercise4)

In the `exercise4` branch, the application introduces a **circuit breaker pattern** around the retries to prevent failure propagation and apply back pressure.

Ensure that you have switched to the `exercise4` branch:

```
git checkout exercise4
```

#### Circuit Breaker Pattern

The **circuit breaker** is a stability pattern used to stop overwhelming a failing service. When a certain number of consecutive failures occur (such as database timeouts), the circuit breaker “trips,” preventing further retries for a short period. This helps avoid continuous failure propagation and allows the service to recover.

You can simulate a failure scenario by adding a toxic that causes timeouts for 100% of the traffic, forcing the circuit breaker to trip:

```
toxiproxy-cli toxic add -t timeout --tox 1 -a "timeout=100" mysql_proxy
```

Explanation:
- **`-t timeout`**: Specifies that a timeout toxic is being added.
- **`--tox 1`**: Applies the timeout to 100% of the traffic.
- **`-a "timeout=100"`**: Causes all affected traffic to timeout after 100ms.

> **Result**: This toxic will cause all database requests to fail (timeout after 100ms). After a certain number of failures, the circuit breaker will trip, stopping further requests and protecting the application from continuous failures.

#### Observing Circuit Breaker Behavior

Once the timeout toxic is applied, run your application and observe how it handles the failures. The circuit breaker should eventually trip after detecting several consecutive failures, and the application will stop sending requests until the circuit breaker resets.

The application should respond quickly with an error once the circuit breaker is tripped, instead of retrying the database calls repeatedly.

### Step 8: Testing the Health Endpoint (exercise5)

In the `exercise5` branch, a **health endpoint** is introduced to monitor the status of both the MySQL connection and the circuit breaker.

Ensure that you have switched to the `exercise5` branch:

```
git checkout exercise5
```

#### Health Endpoint Overview

The health endpoint is accessible at **`/health/`** and provides a JSON response that includes the health status of the system. The two components being monitored are:

1. **MySQL Connection**: The status of the MySQL database connection.
2. **Circuit Breaker**: The status of the circuit breaker (whether it's open or closed).

The JSON output from the endpoint looks like this:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "mysql",
      "status": "Healthy",
      "description": "",
      "duration": 4.7782
    },
    {
      "name": "circuitBreaker",
      "status": "Healthy",
      "description": "Circuit breaker is currently Closed.",
      "duration": 0.0418
    }
  ],
  "totalDuration": 4.8251
}
```

#### Fields Explained

- `status`: Overall system status (e.g., "Healthy" or "Unhealthy").
- `checks`: A list of individual checks, including:
   - `name`: The name of the service being checked (e.g., "mysql" or "circuitBreaker").
   - `status`: The health status of that service (e.g., "Healthy" or "Unhealthy").
   - `description`: Additional information (e.g., circuit breaker status).
   - `duration`: The time taken to complete the health check (in milliseconds).
- `totalDuration`: The total time taken for all health checks.

#### Testing the Health Endpoint

To check the health of the system, query the `/health/` endpoint:

- Using curl:
   
   ```
   curl http://localhost:5000/health/
   ```

- Using a Browser: Simply navigate to `http://localhost:PORT_NUMBER/health/`.


#### Monitoring Health for Chaos Engineering

During chaos engineering experiments (e.g., introducing network latency, timeouts, or circuit breaker tripping), this health endpoint can be used to monitor the real-time status of the system and verify its resilience.

### Step 9: Verify the Proxy

To list active proxies and ensure the mysql_proxy was created successfully:

```
toxiproxy-cli list
```

### Step 10: Test the Connection to MySQL via Toxiproxy

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

### Updating the Proxy

To update the latency or remove it, you can use the following commands:

- **Update the latency**:
  
  ```
  toxiproxy-cli toxic update mysql_proxy --name latency --attribute latency=500
  ```

- **Remove the toxic**:
  
  ```
  toxiproxy-cli toxic remove mysql_proxy --name latency
  ```

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
