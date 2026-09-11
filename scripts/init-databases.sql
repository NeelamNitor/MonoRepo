-- Run automatically by the postgres container on first init (docker-compose.yml).
-- Mirrors the two databases created manually in this dev environment (architecture.md §3.7 — database-per-service).
CREATE DATABASE order_db;
CREATE DATABASE product_db;
