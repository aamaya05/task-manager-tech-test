-- Seed demo users
-- demo@taskmanager.io  → Demo1234!
-- admin@taskmanager.io → Admin1234!

INSERT INTO users (id, username, email, password_hash, created_at)
VALUES
  ('a1b2c3d4-0001-0001-0001-000000000001', 'demo_user', 'demo@taskmanager.io',
   '$2a$12$rWSAn7AO3./wkrOVXgmYyeu4o2FVto4.teGCv8GGCQiS04o0TEVR6', NOW()),
  ('a1b2c3d4-0002-0002-0002-000000000002', 'admin', 'admin@taskmanager.io',
   '$2a$12$rX68cCN1NXTefXMA2DSINu8Ugx8TzrdQ7UImi9/RAU4Hs/PB8nriS', NOW())
ON CONFLICT (email) DO NOTHING;

-- Seed sample tasks for demo_user
INSERT INTO tasks (id, title, description, status, due_date, user_id, created_at, updated_at)
VALUES
  (gen_random_uuid(), 'Solid Principles', 'Get better improving your code quality', 'Done',
   NOW() - INTERVAL '5 days', 'a1b2c3d4-0001-0001-0001-000000000001', NOW() - INTERVAL '7 days', NOW() - INTERVAL '5 days'),
  (gen_random_uuid(), 'Implement domain layer', 'Create entities, value objects, and repository interfaces', 'Done',
   NOW() - INTERVAL '3 days', 'a1b2c3d4-0001-0001-0001-000000000001', NOW() - INTERVAL '6 days', NOW() - INTERVAL '3 days'),
  (gen_random_uuid(), 'Test', 'Testing', 'InProgress',
   NOW() + INTERVAL '2 days', 'a1b2c3d4-0001-0001-0001-000000000001', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
  (gen_random_uuid(), 'Angular', 'Learn how to use angular', 'InProgress',
   NOW() + INTERVAL '5 days', 'a1b2c3d4-0001-0001-0001-000000000001', NOW() - INTERVAL '1 day', NOW())
ON CONFLICT DO NOTHING;
