-- Loads questions.csv + options.csv (made by export.sql) into PRODUCTION.
--
-- Run from this folder, with the production URL in an environment variable:
--   dry run (default — reports, then rolls back; writes nothing):
--     psql "$PROD_DB_URL" -f import.sql
--   real run:
--     psql "$PROD_DB_URL" -v commit=true -f import.sql
--
-- Guarantees:
--   • One transaction: everything lands, or nothing does.
--   • Aborts if any subject / difficulty / complexity / marks / question type
--     named in the files does not exist on production.
--   • Creates missing chapter topics (the chapters NTA dropped in 2024).
--   • Skips questions already there — same id, or same text — so re-running is harmless.
--   • Everything is inserted as Draft (status 1); students see nothing until published.

\set ON_ERROR_STOP on
\encoding UTF8
\if :{?commit}
\else
  \set commit false
\endif

begin;

create temp table src_q (
  id uuid, text text, solution text, subtopic varchar(200), question_type text,
  subject text, topic text, topic_sort_order int, difficulty text, complexity text,
  marks text, negative_marks text, numerical_answer numeric, numerical_tolerance numeric,
  assertion_text text, reason_text text, is_ai_generated boolean, created_at timestamptz
) on commit drop;

create temp table src_o (
  question_id uuid, label char(1), text text, is_correct boolean, sort_order int
) on commit drop;

\copy src_q from 'questions.csv' with (format csv, header, encoding 'UTF8')
\copy src_o from 'options.csv'   with (format csv, header, encoding 'UTF8')

\echo
\echo '--- 1. Names in the files that production does not have (must be empty) ---'
create temp table missing on commit drop as
          select distinct 'subject'        as kind, subject        as name from src_q where subject        not in (select name from subjects)
union all select distinct 'question type',          question_type          from src_q where question_type  not in (select name from question_types)
union all select distinct 'difficulty',             difficulty             from src_q where difficulty     not in (select name from difficulty_levels)
union all select distinct 'complexity',             complexity             from src_q where complexity     not in (select name from complexity_levels)
union all select distinct 'marks',                  marks                  from src_q where marks          not in (select name from marks_master)
union all select distinct 'negative marks',         negative_marks         from src_q where negative_marks not in (select name from negative_marks_master);
select * from missing order by kind, name;

do $$ begin
  if exists (select 1 from missing) then
    raise exception 'Production is missing the master names listed above — nothing was written.';
  end if;
end $$;

\echo '--- 2. Chapter topics that will be created ---'
create temp table new_topics on commit drop as
select distinct s.id as subject_id, q.topic as name, q.topic_sort_order as sort_order
from src_q q
join subjects s on s.name = q.subject
where not exists (select 1 from topics t
                  where t.subject_id = s.id and lower(t.name) = lower(q.topic));
select name, sort_order from new_topics order by sort_order, name;

insert into topics (subject_id, name, is_active, sort_order)
select subject_id, name, true, sort_order from new_topics;

\echo '--- 3. What will be copied ---'
create temp table to_insert on commit drop as
select q.* from src_q q
where not exists (select 1 from questions x where x.id = q.id)
  and not exists (select 1 from questions x where lower(btrim(x.text)) = lower(btrim(q.text)));

select (select count(*) from src_q)                                as in_file,
       (select count(*) from src_q q where exists
           (select 1 from questions x where x.id = q.id))            as already_there_same_id,
       (select count(*) from src_q) - (select count(*) from to_insert)
       - (select count(*) from src_q q where exists
           (select 1 from questions x where x.id = q.id))            as skipped_same_text,
       (select count(*) from to_insert)                             as to_insert,
       (select count(*) from src_o o join to_insert q on q.id = o.question_id) as options_to_insert;

insert into questions (id, text, solution, subtopic, question_type_id, status, subject_id, topic_id,
                       difficulty_level_id, complexity_level_id, marks_id, negative_marks_id,
                       "ExamTypeId", numerical_answer, numerical_tolerance, assertion_text, reason_text,
                       passage_id, created_at, updated_at, created_by, updated_by, is_ai_generated)
select q.id, q.text, q.solution, q.subtopic, qt.id, 1, s.id, t.id,
       dl.id, cl.id, mm.id, nm.id,
       null, q.numerical_answer, q.numerical_tolerance, q.assertion_text, q.reason_text,
       null, q.created_at, now(),
       (select id from users where lower(email) = 'admin@gridacademy.com' limit 1), null,
       q.is_ai_generated
from to_insert q
join subjects              s  on s.name  = q.subject
join topics                t  on t.subject_id = s.id and lower(t.name) = lower(q.topic)
join question_types        qt on qt.name = q.question_type
join difficulty_levels     dl on dl.name = q.difficulty
join complexity_levels     cl on cl.name = q.complexity
join marks_master          mm on mm.name = q.marks
join negative_marks_master nm on nm.name = q.negative_marks;

insert into question_options (question_id, label, text, is_correct, sort_order)
select o.question_id, o.label, o.text, o.is_correct, o.sort_order
from src_o o join to_insert q on q.id = o.question_id;

\echo '--- 4. Per chapter, as it would stand ---'
select t.name as chapter, count(*) as questions_added
from to_insert q
join subjects s on s.name = q.subject
join topics t on t.subject_id = s.id and lower(t.name) = lower(q.topic)
group by t.name order by t.name;

-- every row in to_insert must have been inserted (a silent join miss would drop it)
do $$ begin
  if (select count(*) from questions where id in (select id from to_insert))
     <> (select count(*) from to_insert) then
    raise exception 'Some questions failed to resolve and were not inserted — rolled back.';
  end if;
end $$;

\if :commit
  commit;
  \echo '=== COMMITTED — questions are on production as Draft ==='
\else
  rollback;
  \echo '=== DRY RUN — rolled back, nothing was written. Re-run with -v commit=true ==='
\endif
