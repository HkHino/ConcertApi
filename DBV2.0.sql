-- =========================
-- Concert Ticket System DB
-- MySQL 8+
-- Signed BIGINT (matches C# long)
-- Includes: extra seed data + orders
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
-- Seed users (password_hash placeholders; C# seeder replaces)
-- -------------------------
INSERT INTO users (email, password_hash, role) VALUES
('admin@concert.local', 'HASH_ME', 'Admin'),
('user1@concert.local', 'HASH_ME', 'User'),
('user2@concert.local', 'HASH_ME', 'User');

-- -------------------------
-- Base concerts (4)
-- -------------------------
INSERT INTO concerts (title, artist, venue, city, concert_date, description) VALUES
('Metal Night', 'Iron Hammer', 'Royal Arena', 'Copenhagen', '2026-11-10 20:00:00', 'Heavy metal showcase.'),
('Synth Dreams', 'Neon Skyline', 'Vega', 'Copenhagen', '2026-10-05 19:30:00', 'Synthwave and retro vibes.'),
('Jazz & Chill', 'Blue Quartet', 'Musikhuset', 'Aarhus', '2026-09-22 18:00:00', 'Modern jazz evening.'),
('Pop Festival', 'Starline', 'Arena Odense', 'Odense', '2026-08-30 17:00:00', 'Pop festival with support acts.');

-- Base tickets (20)
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

-- ============================================================
-- EXTRA SEEDING FOR PAGINATION DEMO
-- +30 concerts, +12 tickets per new concert (=360 tickets)
-- ============================================================

DROP TEMPORARY TABLE IF EXISTS tmp_concert_seed;
CREATE TEMPORARY TABLE tmp_concert_seed (
  title        VARCHAR(200) NOT NULL,
  artist       VARCHAR(200) NOT NULL,
  venue        VARCHAR(200) NOT NULL,
  city         VARCHAR(120) NOT NULL,
  concert_date DATETIME NOT NULL,
  description  TEXT NULL,
  base_price   DECIMAL(10,2) NOT NULL
);

INSERT INTO tmp_concert_seed (title, artist, venue, city, concert_date, description, base_price) VALUES
('Midnight Metal', 'Iron Hammer', 'Royal Arena', 'Copenhagen', '2026-07-05 20:00:00', 'Loud and heavy.', 499.00),
('Neon Nights', 'Neon Skyline', 'Vega', 'Copenhagen', '2026-07-12 19:30:00', 'Synthwave evening.', 349.00),
('Jazz Sundays', 'Blue Quartet', 'Musikhuset', 'Aarhus', '2026-07-20 18:00:00', 'Smooth jazz set.', 299.00),
('Indie Pulse', 'Paper Planes', 'Train', 'Aarhus', '2026-07-26 20:00:00', 'Indie rock show.', 289.00),
('Pop Heat', 'Starline', 'Arena Odense', 'Odense', '2026-08-02 17:00:00', 'Pop hits live.', 399.00),
('Techno Warehouse', 'DJ Aurora', 'Tap1', 'Copenhagen', '2026-08-08 23:00:00', 'Late night techno.', 279.00),
('Acoustic Evening', 'Luna & Co', 'Hotel Cecil', 'Copenhagen', '2026-08-14 19:00:00', 'Acoustic session.', 249.00),
('Rock Revival', 'The Loud Ones', 'Aalborg Kongres', 'Aalborg', '2026-08-16 20:00:00', 'Classic rock tribute.', 329.00),
('Folk Stories', 'Nordic Tales', 'Musik i Lejet', 'Helsingør', '2026-08-21 18:30:00', 'Folk festival vibes.', 259.00),
('Hip-Hop Night', 'MC North', 'Pumpehuset', 'Copenhagen', '2026-08-23 20:00:00', 'Hip-hop showcase.', 299.00),
('Classical Gala', 'City Orchestra', 'DR Koncerthuset', 'Copenhagen', '2026-08-28 19:30:00', 'Orchestral gala.', 449.00),
('Summer Beats', 'DJ Aurora', 'Vega', 'Copenhagen', '2026-09-03 22:00:00', 'Dance night.', 269.00),
('Metalcore Mayhem', 'Riftwalk', 'Train', 'Aarhus', '2026-09-05 20:00:00', 'Metalcore lineup.', 319.00),
('Soul & RnB', 'Velvet Voice', 'Musikhuset', 'Aarhus', '2026-09-10 19:00:00', 'Soul night.', 299.00),
('Comedy + Music', 'Stage Friends', 'Bremen Teater', 'Copenhagen', '2026-09-12 20:00:00', 'Mixed show.', 259.00),
('Latin Party', 'Salsa Storm', 'Tap1', 'Copenhagen', '2026-09-18 21:00:00', 'Latin dance party.', 289.00),
('EDM Festival', 'Various DJs', 'Royal Arena', 'Copenhagen', '2026-09-20 16:00:00', 'EDM day festival.', 599.00),
('Piano Night', 'Isabella Keys', 'Hotel Cecil', 'Copenhagen', '2026-09-24 19:30:00', 'Solo piano set.', 279.00),
('Bluegrass Live', 'String Riders', 'Train', 'Aarhus', '2026-09-26 19:00:00', 'Bluegrass show.', 239.00),
('Hard Rock Live', 'Steel Avenue', 'Arena Odense', 'Odense', '2026-09-28 20:00:00', 'Hard rock night.', 349.00),

('Autumn Pop', 'Starline', 'Royal Arena', 'Copenhagen', '2026-10-02 19:30:00', 'Autumn tour.', 399.00),
('Indie Sessions', 'Paper Planes', 'Vega', 'Copenhagen', '2026-10-06 20:00:00', 'New album songs.', 279.00),
('Jazz Fusion', 'Blue Quartet', 'Musikhuset', 'Aarhus', '2026-10-10 18:30:00', 'Fusion set.', 319.00),
('Night of Techno', 'DJ Aurora', 'Pumpehuset', 'Copenhagen', '2026-10-15 23:00:00', 'Hard techno.', 299.00),
('Rock in Aalborg', 'The Loud Ones', 'Aalborg Kongres', 'Aalborg', '2026-10-17 20:00:00', 'Tour stop.', 339.00),
('Folk Evening', 'Nordic Tales', 'Bremen Teater', 'Copenhagen', '2026-10-20 19:00:00', 'Folk evening.', 249.00),
('Hip-Hop Cypher', 'MC North', 'Train', 'Aarhus', '2026-10-22 20:00:00', 'Cypher night.', 289.00),
('Classical Strings', 'City Orchestra', 'DR Koncerthuset', 'Copenhagen', '2026-10-26 19:30:00', 'String program.', 429.00),
('Metal Legends', 'Iron Hammer', 'Arena Odense', 'Odense', '2026-10-30 20:00:00', 'Big metal night.', 529.00),
('Halloween Synth', 'Neon Skyline', 'Vega', 'Copenhagen', '2026-10-31 21:00:00', 'Halloween special.', 369.00);

-- Insert extra concerts
INSERT INTO concerts (title, artist, venue, city, concert_date, description)
SELECT title, artist, venue, city, concert_date, description
FROM tmp_concert_seed;

-- Generate 12 tickets per extra concert
-- Generate 12 tickets per extra concert (no recursive CTE required)
INSERT INTO tickets (concert_id, price, seat_number, status)
SELECT
  c.id AS concert_id,
  s.base_price AS price,
  CONCAT('S-', LPAD(n.n, 2, '0')) AS seat_number,
  'Available' AS status
FROM concerts c
JOIN tmp_concert_seed s
  ON c.title = s.title
 AND c.artist = s.artist
 AND c.concert_date = s.concert_date
JOIN (
  SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
  UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8
  UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
) n
ORDER BY c.id, n.n;

-- ============================================================
-- SEED SOME ORDERS (SOLD TICKETS) FOR ORDER PAGINATION
-- Creates 15 orders and marks those tickets Sold.
-- user_id values assume your 3 seeded users are:
-- 1 = admin, 2 = user1, 3 = user2 (because inserted in that order)
-- ============================================================

DROP TEMPORARY TABLE IF EXISTS tmp_sold;
CREATE TEMPORARY TABLE tmp_sold (
  ticket_id BIGINT NOT NULL,
  user_id   BIGINT NOT NULL
);

-- Give first 10 available tickets to user1 (id=2)
INSERT INTO tmp_sold (ticket_id, user_id)
SELECT id, 2
FROM tickets
WHERE status = 'Available'
ORDER BY id
LIMIT 10;

-- Give next 5 available tickets to user2 (id=3)
INSERT INTO tmp_sold (ticket_id, user_id)
SELECT id, 3
FROM tickets
WHERE status = 'Available'
ORDER BY id
LIMIT 5 OFFSET 10;

-- Mark those tickets as Sold
UPDATE tickets t
JOIN tmp_sold s ON s.ticket_id = t.id
SET t.status = 'Sold';

-- Create orders for those tickets
INSERT INTO orders (user_id, ticket_id, status, purchased_at)
SELECT user_id, ticket_id, 'Active', NOW()
FROM tmp_sold;

-- -------------------------
-- Quick counts for sanity
-- -------------------------
SELECT
  (SELECT COUNT(*) FROM concerts) AS total_concerts,
  (SELECT COUNT(*) FROM tickets) AS total_tickets,
  (SELECT COUNT(*) FROM tickets WHERE status='Available') AS available_tickets,
  (SELECT COUNT(*) FROM tickets WHERE status='Sold') AS sold_tickets,
  (SELECT COUNT(*) FROM orders) AS total_orders;