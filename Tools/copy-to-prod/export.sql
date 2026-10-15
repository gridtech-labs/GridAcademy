-- Exports the Chemistry PYQ questions from the LOCAL database into two CSV files,
-- with every foreign key written as a NAME so import.sql can resolve it against
-- production, whose ids differ.
--
-- Run from this folder:
--   psql -h localhost -U postgres -d gridacademy -f export.sql
--
-- Scope: Chemistry questions created by the PYQ book imports (2026-10-07 onward).
-- The 16 older Chemistry rows are deliberately left out.

\set ON_ERROR_STOP on
\encoding UTF8

create temp view export_questions as
select q.id,
       q.text,
       q.solution,
       q.subtopic,
       qt.name  as question_type,
       s.name   as subject,
       t.name   as topic,
       t.sort_order as topic_sort_order,
       dl.name  as difficulty,
       cl.name  as complexity,
       mm.name  as marks,
       nm.name  as negative_marks,
       q.numerical_answer,
       q.numerical_tolerance,
       q.assertion_text,
       q.reason_text,
       q.is_ai_generated,
       q.created_at
from questions q
join question_types        qt on qt.id = q.question_type_id
join subjects              s  on s.id  = q.subject_id
join topics                t  on t.id  = q.topic_id
join difficulty_levels     dl on dl.id = q.difficulty_level_id
join complexity_levels     cl on cl.id = q.complexity_level_id
join marks_master          mm on mm.id = q.marks_id
join negative_marks_master nm on nm.id = q.negative_marks_id
where s.name = 'Chemistry'
  and q.created_at >= '2026-10-07';

create temp view export_options as
select o.question_id, o.label, o.text, o.is_correct, o.sort_order
from question_options o
where o.question_id in (select id from export_questions);

\copy (select * from export_questions order by created_at, id) to 'questions.csv' with (format csv, header, encoding 'UTF8')
\copy (select * from export_options order by question_id, sort_order) to 'options.csv' with (format csv, header, encoding 'UTF8')

select (select count(*) from export_questions) as questions_exported,
       (select count(*) from export_options)   as options_exported;
