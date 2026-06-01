-- =========================
-- Concert Ticket System DB
-- MySQL 8+
-- Signed BIGINT (matches C# long)
-- =========================

DROP DATABASE IF EXISTS concertdb;
CREATE DATABASE concertdb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE concertdb;

-- -------------------------
-- USERS
-- -------------------------
CREATE TABLE users (
  id            BIGINT NOT NULL AUTO_INCREMENT,
  email         VARCHAR(255) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  role          ENUM('User','Admin') NOT NULL DEFAULT 'User',
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_users_email (email)
);

-- -------------------------
-- CONCERTS
-- -------------------------
CREATE TABLE concerts (
  id           BIGINT NOT NULL AUTO_INCREMENT,
  title        VARCHAR(200) NOT NULL,
  artist       VARCHAR(200) NOT NULL,
  venue        VARCHAR(200) NOT NULL,
  city         VARCHAR(120) NOT NULL,
  concert_date DATETIME NOT NULL,
  description  TEXT NULL,
  created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_concerts_city_date (city, concert_date),
  KEY ix_concerts_artist (artist)
);

-- -------------------------
-- TICKETS
-- -------------------------
CREATE TABLE tickets (
  id          BIGINT NOT NULL AUTO_INCREMENT,
  concert_id  BIGINT NOT NULL,
  price       DECIMAL(10,2) NOT NULL,
  seat_number VARCHAR(50) NOT NULL,
  status      ENUM('Available','Reserved','Sold') NOT NULL DEFAULT 'Available',
  created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_tickets_concert
    FOREIGN KEY (concert_id) REFERENCES concerts(id)
    ON DELETE CASCADE,
  UNIQUE KEY uq_ticket_concert_seat (concert_id, seat_number),
  KEY ix_tickets_concert_status (concert_id, status)
);

-- -------------------------
-- ORDERS (Ticket purchases)
-- -------------------------
CREATE TABLE orders (
  id           BIGINT NOT NULL AUTO_INCREMENT,
  user_id      BIGINT NOT NULL,
  ticket_id    BIGINT NOT NULL,
  status       ENUM('Active','Cancelled') NOT NULL DEFAULT 'Active',
  purchased_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_orders_user
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE RESTRICT,
  CONSTRAINT fk_orders_ticket
    FOREIGN KEY (ticket_id) REFERENCES tickets(id)
    ON DELETE RESTRICT,
  UNIQUE KEY uq_orders_ticket (ticket_id),
  KEY ix_orders_user_date (user_id, purchased_at)
);

-- -------------------------
-- REFRESH TOKENS
-- -------------------------
CREATE TABLE refresh_tokens (
  id         BIGINT NOT NULL AUTO_INCREMENT,
  user_id    BIGINT NOT NULL,
  token_hash VARCHAR(255) NOT NULL,
  expires_at DATETIME NOT NULL,
  revoked_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_refresh_user
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE CASCADE,
  KEY ix_refresh_user (user_id),
  UNIQUE KEY uq_refresh_token_hash (token_hash)
);

-- -------------------------
-- REVOKED TOKENS (key-value store for JWT jti)
-- -------------------------
CREATE TABLE revoked_tokens (
  jti        VARCHAR(64) NOT NULL,
  expires_at DATETIME NOT NULL,
  revoked_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (jti)
);

-- -------------------------
-- Seed data
-- NOTE: password_hash values are placeholders.
-- Your API Seeder will generate BCrypt hashes and update these.
-- -------------------------
INSERT INTO users (email, password_hash, role) VALUES
('admin@concert.local', 'HASH_ME', 'Admin'),
('user1@concert.local', 'HASH_ME', 'User'),
('user2@concert.local', 'HASH_ME', 'User');

INSERT INTO concerts (title, artist, venue, city, concert_date, description) VALUES
('Metal Night', 'Iron Hammer', 'Royal Arena', 'Copenhagen', '2026-11-10 20:00:00', 'Heavy metal showcase.'),
('Synth Dreams', 'Neon Skyline', 'Vega', 'Copenhagen', '2026-10-05 19:30:00', 'Synthwave and retro vibes.'),
('Jazz & Chill', 'Blue Quartet', 'Musikhuset', 'Aarhus', '2026-09-22 18:00:00', 'Modern jazz evening.'),
('Pop Festival', 'Starline', 'Arena Odense', 'Odense', '2026-08-30 17:00:00', 'Pop festival with support acts.');

INSERT INTO tickets (concert_id, price, seat_number, status) VALUES
(1, 499.00, 'A-01', 'Available'),
(1, 499.00, 'A-02', 'Available'),
(1, 499.00, 'A-03', 'Available'),
(1, 499.00, 'A-04', 'Available'),
(1, 499.00, 'A-05', 'Available'),

(2, 349.00, 'B-01', 'Available'),
(2, 349.00, 'B-02', 'Available'),
(2, 349.00, 'B-03', 'Available'),
(2, 349.00, 'B-04', 'Available'),
(2, 349.00, 'B-05', 'Available'),

(3, 299.00, 'C-01', 'Available'),
(3, 299.00, 'C-02', 'Available'),
(3, 299.00, 'C-03', 'Available'),
(3, 299.00, 'C-04', 'Available'),
(3, 299.00, 'C-05', 'Available'),

(4, 399.00, 'D-01', 'Available'),
(4, 399.00, 'D-02', 'Available'),
(4, 399.00, 'D-03', 'Available'),
(4, 399.00, 'D-04', 'Available'),
(4, 399.00, 'D-05', 'Available');