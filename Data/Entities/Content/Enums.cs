namespace GridAcademy.Data.Entities.Content;

/// <summary>
/// All supported question formats. Integer values are persisted in the DB and
/// must match the seeded rows in the question_types master table (id 1-9).
/// </summary>
public enum QuestionType
{
    MCQ               = 1,  // Single-correct MCQ            (was SingleCorrect)
    MSQ               = 2,  // Multiple-select / multi-correct (was MultipleCorrect)
    NAT               = 3,  // Numerical Answer Type          (was Numerical)
    FillInBlanks      = 4,  // Fill-in-the-blank(s)
    TrueFalse         = 5,  // True / False (2-option MCQ)
    MatchTheFollowing = 6,  // Match List-I ↔ List-II (1:1 pairs)
    AssertionReason   = 7,  // Assertion A + Reason R, MCQ-style answer
    PassageBased      = 8,  // Comprehension / paragraph sub-questions
    MatrixMatch       = 9   // Matrix match (many-to-many row × column)
}

public enum QuestionStatus
{
    Draft     = 1,  // Editable; cannot be used in exams
    Published = 2   // Validated; available for test creation
}
