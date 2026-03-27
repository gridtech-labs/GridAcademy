const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType,
  ShadingType, VerticalAlign, PageNumber, PageBreak, LevelFormat,
  ExternalHyperlink
} = require('docx');
const fs = require('fs');

// ── Helpers ──────────────────────────────────────────────────────────────────
const BLUE       = '1A56A0';
const LIGHT_BLUE = 'D5E8F7';
const DARK_BLUE  = '0D3B6E';
const GREEN      = '1E7D45';
const LIGHT_GREEN= 'D6F0E0';
const ORANGE     = 'C05A00';
const LIGHT_ORANGE='FDE9D4';
const GREY_BG    = 'F2F2F2';
const BORDER_CLR = 'CCCCCC';

const PAGE_W = 12240;
const MARGIN = 1080;  // 0.75 inch
const CONTENT_W = PAGE_W - MARGIN * 2; // 10080

function border(color = BORDER_CLR) {
  return { style: BorderStyle.SINGLE, size: 1, color };
}
function cellBorders(color = BORDER_CLR) {
  const b = border(color);
  return { top: b, bottom: b, left: b, right: b };
}

function h1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 400, after: 200 },
    children: [new TextRun({ text, bold: true, color: DARK_BLUE, size: 36, font: 'Arial' })]
  });
}
function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 300, after: 140 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: BLUE, space: 4 } },
    children: [new TextRun({ text, bold: true, color: BLUE, size: 28, font: 'Arial' })]
  });
}
function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 100 },
    children: [new TextRun({ text, bold: true, color: '333333', size: 24, font: 'Arial' })]
  });
}
function para(text, opts = {}) {
  return new Paragraph({
    spacing: { before: 80, after: 80 },
    children: [new TextRun({ text, size: 22, font: 'Arial', ...opts })]
  });
}
function bullet(text, level = 0) {
  return new Paragraph({
    numbering: { reference: 'bullets', level },
    spacing: { before: 60, after: 60 },
    children: [new TextRun({ text, size: 22, font: 'Arial' })]
  });
}
function numbered(text, level = 0) {
  return new Paragraph({
    numbering: { reference: 'numbers', level },
    spacing: { before: 60, after: 60 },
    children: [new TextRun({ text, size: 22, font: 'Arial' })]
  });
}
function spacer(lines = 1) {
  return new Paragraph({ spacing: { before: 60 * lines, after: 60 * lines }, children: [] });
}
function pageBreak() {
  return new Paragraph({ children: [new PageBreak()] });
}

function infoBox(label, text, bgColor = LIGHT_BLUE, labelColor = BLUE) {
  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: [CONTENT_W],
    rows: [new TableRow({ children: [new TableCell({
      borders: cellBorders(labelColor),
      width: { size: CONTENT_W, type: WidthType.DXA },
      shading: { fill: bgColor, type: ShadingType.CLEAR },
      margins: { top: 120, bottom: 120, left: 180, right: 180 },
      children: [
        new Paragraph({ spacing: { before: 40, after: 60 }, children: [
          new TextRun({ text: label, bold: true, color: labelColor, size: 22, font: 'Arial' })
        ]}),
        new Paragraph({ spacing: { before: 40, after: 40 }, children: [
          new TextRun({ text, size: 22, font: 'Arial', color: '222222' })
        ]})
      ]
    })]})],
  });
}

function tableWithHeader(headers, rows, colWidths) {
  const totalW = colWidths.reduce((a, b) => a + b, 0);
  const headerRow = new TableRow({
    tableHeader: true,
    children: headers.map((h, i) => new TableCell({
      borders: cellBorders(BLUE),
      width: { size: colWidths[i], type: WidthType.DXA },
      shading: { fill: BLUE, type: ShadingType.CLEAR },
      margins: { top: 100, bottom: 100, left: 120, right: 120 },
      verticalAlign: VerticalAlign.CENTER,
      children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [
        new TextRun({ text: h, bold: true, color: 'FFFFFF', size: 20, font: 'Arial' })
      ]})]
    }))
  });

  const dataRows = rows.map((row, ri) => new TableRow({
    children: row.map((cell, ci) => new TableCell({
      borders: cellBorders(),
      width: { size: colWidths[ci], type: WidthType.DXA },
      shading: { fill: ri % 2 === 0 ? 'FFFFFF' : GREY_BG, type: ShadingType.CLEAR },
      margins: { top: 80, bottom: 80, left: 120, right: 120 },
      children: [new Paragraph({ children: [
        new TextRun({ text: cell, size: 20, font: 'Arial' })
      ]})]
    }))
  }));

  return new Table({
    width: { size: totalW, type: WidthType.DXA },
    columnWidths: colWidths,
    rows: [headerRow, ...dataRows]
  });
}

function twoColRow(label, value, shade = false) {
  return new TableRow({ children: [
    new TableCell({
      borders: cellBorders(),
      width: { size: 3200, type: WidthType.DXA },
      shading: { fill: shade ? GREY_BG : 'FFFFFF', type: ShadingType.CLEAR },
      margins: { top: 80, bottom: 80, left: 120, right: 120 },
      children: [new Paragraph({ children: [new TextRun({ text: label, bold: true, size: 20, font: 'Arial' })] })]
    }),
    new TableCell({
      borders: cellBorders(),
      width: { size: CONTENT_W - 3200, type: WidthType.DXA },
      shading: { fill: shade ? GREY_BG : 'FFFFFF', type: ShadingType.CLEAR },
      margins: { top: 80, bottom: 80, left: 120, right: 120 },
      children: [new Paragraph({ children: [new TextRun({ text: value, size: 20, font: 'Arial' })] })]
    })
  ]});
}
function twoColTable(pairs) {
  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: [3200, CONTENT_W - 3200],
    rows: pairs.map(([l, v], i) => twoColRow(l, v, i % 2 !== 0))
  });
}

// ── DOCUMENT ─────────────────────────────────────────────────────────────────
const doc = new Document({
  numbering: {
    config: [
      { reference: 'bullets', levels: [
        { level: 0, format: LevelFormat.BULLET, text: '\u2022', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } },
        { level: 1, format: LevelFormat.BULLET, text: '\u25E6', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 1080, hanging: 360 } } } }
      ]},
      { reference: 'numbers', levels: [
        { level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } },
        { level: 1, format: LevelFormat.LOWER_LETTER, text: '%2)', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 1080, hanging: 360 } } } }
      ]}
    ]
  },
  sections: [{
    properties: {
      page: {
        size: { width: PAGE_W, height: 15840 },
        margin: { top: MARGIN, right: MARGIN, bottom: MARGIN, left: MARGIN }
      }
    },
    headers: {
      default: new Header({ children: [
        new Paragraph({
          border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: BLUE, space: 4 } },
          children: [
            new TextRun({ text: 'GridAcademy', bold: true, color: BLUE, size: 20, font: 'Arial' }),
            new TextRun({ text: '  |  Product Specification Document (PSD) v1.0', color: '666666', size: 18, font: 'Arial' })
          ]
        })
      ]})
    },
    footers: {
      default: new Footer({ children: [
        new Paragraph({
          border: { top: { style: BorderStyle.SINGLE, size: 2, color: BLUE, space: 4 } },
          alignment: AlignmentType.RIGHT,
          children: [
            new TextRun({ text: 'Confidential  |  Page ', size: 18, font: 'Arial', color: '888888' }),
            new TextRun({ children: [PageNumber.CURRENT], size: 18, font: 'Arial', color: '888888' }),
            new TextRun({ text: ' of ', size: 18, font: 'Arial', color: '888888' }),
            new TextRun({ children: [PageNumber.TOTAL_PAGES], size: 18, font: 'Arial', color: '888888' })
          ]
        })
      ]})
    },
    children: [

      // ── COVER PAGE ──────────────────────────────────────────────────────────
      spacer(4),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 0, after: 100 }, children: [
        new TextRun({ text: 'GRIDACADEMY', bold: true, color: DARK_BLUE, size: 72, font: 'Arial' })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 0, after: 200 }, children: [
        new TextRun({ text: 'Mock Test Marketplace Platform', color: BLUE, size: 40, font: 'Arial' })
      ]}),
      spacer(1),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [CONTENT_W],
        rows: [new TableRow({ children: [new TableCell({
          borders: cellBorders(BLUE),
          width: { size: CONTENT_W, type: WidthType.DXA },
          shading: { fill: DARK_BLUE, type: ShadingType.CLEAR },
          margins: { top: 300, bottom: 300, left: 400, right: 400 },
          verticalAlign: VerticalAlign.CENTER,
          children: [
            new Paragraph({ alignment: AlignmentType.CENTER, children: [
              new TextRun({ text: 'PRODUCT SPECIFICATION DOCUMENT', bold: true, color: 'FFFFFF', size: 36, font: 'Arial' })
            ]}),
            new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 100 }, children: [
              new TextRun({ text: 'Version 1.0  |  MVP Release  |  March 2026', color: 'AACCEE', size: 24, font: 'Arial' })
            ]})
          ]
        })]})],
      }),
      spacer(3),
      twoColTable([
        ['Document Type', 'Product Specification Document (PSD)'],
        ['Project', 'GridAcademy \u2014 Indian Competitive Exam Mock Test Marketplace'],
        ['Version', 'v1.0 \u2014 MVP Scope'],
        ['Prepared By', 'GridAcademy Product Team'],
        ['Date', 'March 2026'],
        ['Status', 'APPROVED \u2014 Ready for Development'],
        ['Base Codebase', 'Existing GridAcademy ASP.NET Core 8 Platform'],
        ['Target Release', 'Sprint 1\u20136 (12 Weeks from Kickoff)'],
      ]),
      pageBreak(),

      // ── SECTION 1: MVP SCOPE ────────────────────────────────────────────────
      h1('1.  MVP Scope \u2014 The 20% That Delivers 80% of Value'),
      infoBox('Strategic Rationale',
        'The single most valuable thing GridAcademy MVP must prove is: "A student can discover, purchase, and take a high-quality mock test from a third-party provider \u2014 and trust the result." Everything else is iteration. The features below are the minimum set required to validate this marketplace loop.',
        LIGHT_BLUE, BLUE),
      spacer(1),

      h2('1.1  The Core Marketplace Loop (Must Work End-to-End)'),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [800, 2800, 3600, 2880],
        rows: [
          new TableRow({ tableHeader: true, children: [
            new TableCell({ borders: cellBorders(BLUE), shading: { fill: BLUE, type: ShadingType.CLEAR }, margins: { top:100,bottom:100,left:120,right:120 }, width:{size:800,type:WidthType.DXA}, children:[new Paragraph({ alignment:AlignmentType.CENTER, children:[new TextRun({text:'#',bold:true,color:'FFFFFF',size:20,font:'Arial'})]})] }),
            new TableCell({ borders: cellBorders(BLUE), shading: { fill: BLUE, type: ShadingType.CLEAR }, margins: { top:100,bottom:100,left:120,right:120 }, width:{size:2800,type:WidthType.DXA}, children:[new Paragraph({ children:[new TextRun({text:'Loop Step',bold:true,color:'FFFFFF',size:20,font:'Arial'})]})] }),
            new TableCell({ borders: cellBorders(BLUE), shading: { fill: BLUE, type: ShadingType.CLEAR }, margins: { top:100,bottom:100,left:120,right:120 }, width:{size:3600,type:WidthType.DXA}, children:[new Paragraph({ children:[new TextRun({text:'What Must Work',bold:true,color:'FFFFFF',size:20,font:'Arial'})]})] }),
            new TableCell({ borders: cellBorders(BLUE), shading: { fill: BLUE, type: ShadingType.CLEAR }, margins: { top:100,bottom:100,left:120,right:120 }, width:{size:2880,type:WidthType.DXA}, children:[new Paragraph({ children:[new TextRun({text:'Reuse Existing?',bold:true,color:'FFFFFF',size:20,font:'Arial'})]})] }),
          ]}),
          ...[ ['1','Provider uploads test', 'CSV import \u2192 test creation \u2192 pricing \u2192 submit for review', '\u2705 CSV import exists; add pricing + review workflow'],
               ['2','Admin reviews & publishes', 'Admin sees pending tests, approves/rejects with notes', '\u2705 Admin panel exists; add review queue'],
               ['3','Student discovers test', 'Homepage + category filter + search by exam name', '\u26A0 New storefront pages needed'],
               ['4','Student tries free test', 'Take a free/trial test without payment', '\u2705 Test engine exists'],
               ['5','Student purchases test', 'Razorpay checkout \u2192 order confirmed \u2192 access granted', '\u274C New \u2014 payment + entitlement'],
               ['6','Student takes paid test', 'Timed exam, negative marking, auto-submit', '\u2705 Test engine exists'],
               ['7','Student sees results', 'Score, rank, subject breakdown, answer key', '\u26A0 Enhance existing result page'],
               ['8','Provider sees earnings', 'Dashboard: sales count, revenue, payout status', '\u274C New \u2014 provider portal'],
          ].map(([n,s,w,r],i) => new TableRow({ children: [
            new TableCell({ borders: cellBorders(), shading:{fill:i%2===0?'FFFFFF':GREY_BG,type:ShadingType.CLEAR}, margins:{top:80,bottom:80,left:120,right:120}, width:{size:800,type:WidthType.DXA}, children:[new Paragraph({alignment:AlignmentType.CENTER,children:[new TextRun({text:n,bold:true,size:20,font:'Arial'})]})] }),
            new TableCell({ borders: cellBorders(), shading:{fill:i%2===0?'FFFFFF':GREY_BG,type:ShadingType.CLEAR}, margins:{top:80,bottom:80,left:120,right:120}, width:{size:2800,type:WidthType.DXA}, children:[new Paragraph({children:[new TextRun({text:s,size:20,font:'Arial'})]})] }),
            new TableCell({ borders: cellBorders(), shading:{fill:i%2===0?'FFFFFF':GREY_BG,type:ShadingType.CLEAR}, margins:{top:80,bottom:80,left:120,right:120}, width:{size:3600,type:WidthType.DXA}, children:[new Paragraph({children:[new TextRun({text:w,size:20,font:'Arial'})]})] }),
            new TableCell({ borders: cellBorders(), shading:{fill:i%2===0?'FFFFFF':GREY_BG,type:ShadingType.CLEAR}, margins:{top:80,bottom:80,left:120,right:120}, width:{size:2880,type:WidthType.DXA}, children:[new Paragraph({children:[new TextRun({text:r,size:19,font:'Arial'})]})] }),
          ]}))
        ]
      }),
      spacer(1),

      h2('1.2  MVP Feature List (INCLUDE vs EXCLUDE)'),
      tableWithHeader(
        ['Feature','Priority','In MVP v1.0?','Sprint','Notes'],
        [
          ['Student Registration (Email + OTP)','P0','YES \u2705','1','New \u2014 OTP via MSG91'],
          ['Student Login / Session','P0','YES \u2705','1','JWT + Cookie'],
          ['Provider Registration + Basic KYC','P0','YES \u2705','1','Name, PAN, Bank a/c'],
          ['Admin: Review & Publish Tests','P0','YES \u2705','1','Extend existing admin'],
          ['Homepage \u2014 Browse Tests by Exam','P0','YES \u2705','2','New storefront'],
          ['Exam Category Filter (10 exams)','P0','YES \u2705','2','Master data exists'],
          ['Test Detail Page (Buy/Try CTA)','P0','YES \u2705','2','New page'],
          ['Free Trial Test (no payment)','P0','YES \u2705','2','Existing test engine'],
          ['Razorpay Payment (INR only)','P0','YES \u2705','3','New integration'],
          ['Entitlement \u2014 grant access post-pay','P0','YES \u2705','3','New enrollment logic'],
          ['Timed Test Engine (existing)','P0','YES \u2705','3','Already built'],
          ['Negative Marking Config','P0','YES \u2705','3','Already built'],
          ['Result Page \u2014 Score + Rank + Breakdown','P0','YES \u2705','4','Enhance existing'],
          ['Provider: Upload Test (CSV)','P0','YES \u2705','4','CSV import exists'],
          ['Provider: Set Pricing per Test','P0','YES \u2705','4','New UI on provider portal'],
          ['Provider: Sales Dashboard','P1','YES \u2705','5','New provider portal page'],
          ['Commission Calculation Engine','P1','YES \u2705','5','New \u2014 70/30 split'],
          ['Leaderboard per Test','P1','YES \u2705','5','New'],
          ['Basic Search (by exam name)','P1','YES \u2705','5','PostgreSQL full-text'],
          ['Email Confirmation (booking)','P1','YES \u2705','5','SendGrid \u2014 Hangfire job'],
          ['Mobile-Responsive Design','P0','YES \u2705','All','Bootstrap 5 responsive'],
          ['Admin Revenue Dashboard','P1','YES \u2705','6','Extend existing admin'],
          ['Subscription/Monthly Pass','P2','NO \u274C','Post-MVP','Phase 2'],
          ['Native Mobile App','P2','NO \u274C','Post-MVP','6+ months'],
          ['Regional Language (Hindi)','P2','NO \u274C','Post-MVP','Phase 2'],
          ['B2B / Coaching Institute License','P2','NO \u274C','Post-MVP','Phase 2'],
          ['Video Solutions / Explanations','P2','NO \u274C','Post-MVP','VL module exists'],
          ['Referral / Affiliate Program','P2','NO \u274C','Post-MVP','Phase 2'],
          ['Bank Offers / Cashback','P2','NO \u274C','Post-MVP','Phase 2'],
          ['Advanced Anti-Cheating (proctoring)','P3','NO \u274C','Post-MVP','Phase 3'],
        ],
        [2600, 900, 1500, 1000, 4080]
      ),
      pageBreak(),

      // ── SECTION 2: EXAM COVERAGE ────────────────────────────────────────────
      h1('2.  v1.0 Exam Coverage \u2014 10 Exams'),
      infoBox('Decision', 'Launch with 10 high-volume government exams that collectively cover 80%+ of the active aspirant base. Focus on exams with clear exam patterns, negative marking, and multiple sections \u2014 where our test engine advantage is strongest.', LIGHT_GREEN, GREEN),
      spacer(1),
      tableWithHeader(
        ['#','Exam Name','Conducting Body','Approx. Applicants/Year','Sections','Marking'],
        [
          ['1','SSC CGL (Combined Graduate Level)','Staff Selection Commission','30 lakh+','GK, Reasoning, Maths, English','+2 / -0.5'],
          ['2','SSC CHSL (Combined Higher Secondary)','Staff Selection Commission','40 lakh+','GK, Reasoning, Maths, English','+2 / -0.5'],
          ['3','RRB NTPC (Non-Technical Popular Categories)','Railway Recruitment Board','1.2 crore+','GK, Maths, Reasoning','+1 / -0.33'],
          ['4','IBPS PO (Probationary Officer)','IBPS','10 lakh+','Reasoning, Quant, English, GK, Computer','+1 / -0.25'],
          ['5','IBPS Clerk','IBPS','15 lakh+','Reasoning, Quant, English','+1 / -0.25'],
          ['6','SBI PO','State Bank of India','20 lakh+','Reasoning, Quant, English, GK','+1 / -0.25'],
          ['7','UPSC Prelims (GS Paper I & II)','UPSC','10 lakh+','GS I, CSAT','+2 / -0.66'],
          ['8','RRB Group D','Railway Recruitment Board','1.15 crore+','GK, Maths, Reasoning, Science','+1 / -0.33'],
          ['9','Delhi Police Constable','Delhi Police','3 lakh+','GK, Reasoning, Maths, English','+1 / -0.25'],
          ['10','UP Police Constable / SI','UP Police','25 lakh+','GK, Hindi, Numerical, Reasoning','+2 / -0.5'],
        ],
        [600, 2800, 2200, 1500, 1800, 1180]
      ),
      spacer(1),
      infoBox('Phase 2 Exam Expansion (Post-MVP)',
        'NEET UG, JEE Mains, CDS, NDA, SEBI Grade A, RBI Grade B, State PSC (Maharashtra/Rajasthan/Bihar), CTET, NVS/KVS Teachers, Insurance (LIC AAO), NABARD.',
        LIGHT_ORANGE, ORANGE),
      pageBreak(),

      // ── SECTION 3: PERSONAS ─────────────────────────────────────────────────
      h1('3.  User Personas'),

      h2('3.1  Persona: Aspirant (Student)'),
      twoColTable([
        ['Name','Rahul Verma'],
        ['Age','24'],
        ['Location','Allahabad, UP (Tier 2 city)'],
        ['Education','B.Sc. Graduate, 2024'],
        ['Device','Android smartphone (primary), PC occasionally'],
        ['Internet','100-200 Mbps Jio; sometimes patchy at home'],
        ['Goal','Clear SSC CGL 2026 in first attempt'],
        ['Pain Points','Cannot afford offline coaching (\u20B920K+/month); free YouTube lacks structured mock tests; existing apps too expensive or limited tests'],
        ['Motivation','Government job = job security + family prestige + steady income'],
        ['Willingness to Pay','Up to \u20B9500/year for quality test series; \u20B999 for a single series if trusted'],
        ['Key Feature Need','Detailed analytics after each test, all-India rank, subject-wise accuracy breakdown'],
      ]),
      spacer(1),

      h2('3.2  Persona: Test Provider (Content Creator)'),
      twoColTable([
        ['Name','Career Pathshala Institute, Jaipur'],
        ['Type','Medium-sized offline coaching institute (150 students/batch)'],
        ['Staff','Director + 2 subject teachers who create content'],
        ['Current Pain','Content sits on paper / WhatsApp; no digital revenue channel'],
        ['Goal','Monetize existing question bank digitally; reach students beyond Jaipur'],
        ['Tech Comfort','Moderate \u2014 comfortable with Excel, basic web apps'],
        ['Expected Revenue','Wants 65-70% of each sale; monthly bank transfer'],
        ['Key Concern','Will GridAcademy steal their content? Will students trust a new platform?'],
        ['Onboarding Need','Simple CSV upload, clear revenue dashboard, quick payout cycle'],
      ]),
      spacer(1),

      h2('3.3  Persona: Platform Admin (GridAcademy Staff)'),
      twoColTable([
        ['Name','Priya \u2014 Operations Head'],
        ['Responsibility','Content review, provider onboarding, dispute resolution, revenue monitoring'],
        ['Tools Needed','Admin panel: review queue, provider list, commission ledger, refund management'],
        ['Volume Expectation','Review 20-30 test uploads/week at launch; scale to 100+/week by Year 2'],
      ]),
      pageBreak(),

      // ── SECTION 4: SCREENS ──────────────────────────────────────────────────
      h1('4.  Screen-by-Screen Specification'),

      h2('4.1  Student-Facing Website (Mobile-Responsive)'),

      h3('Screen 1: Homepage'),
      tableWithHeader(
        ['Element','Description','Data Source'],
        [
          ['Top Navigation','Logo | Search bar | "Login/Sign Up" button | City selector (future)','Static + API'],
          ['Category Bar','Horizontal scroll: SSC | RRB | Banking | UPSC | Police | Defence | + more','ExamType master table'],
          ['Hero Banner','Full-width CMS banner; configurable from admin (event, offer, new launch)','Admin CMS table'],
          ['"Free Tests" Section','Horizontal card carousel \u2014 tests marked IsFreePreview=true','Tests API \u2014 filter free'],
          ['"Top Selling" Section','Tests sorted by PurchaseCount desc','Tests API \u2014 sorted'],
          ['"New Arrivals" Section','Tests sorted by PublishedAt desc (last 30 days)','Tests API \u2014 date filter'],
          ['Test Card','Thumbnail | Exam badge | Title | Provider name | Rating | Price | "Try Free" / "Buy" CTA','Tests API'],
          ['Footer','About, Contact, Privacy Policy, Terms, Grievance Officer, GST No.','Static'],
        ],
        [2000, 5280, 2800]
      ),
      spacer(1),

      h3('Screen 2: Exam Category Listing Page (e.g., /exams/ssc-cgl)'),
      tableWithHeader(
        ['Element','Description','Notes'],
        [
          ['Page Title','SSC CGL Mock Tests | GridAcademy (SEO title)','SSR for SEO'],
          ['Filter Panel','Sidebar: Price (Free/Paid), Rating (4+/3+), Language (EN/HI), Difficulty','Client-side filter'],
          ['Sort Bar','Popular | Newest | Price Low-High | Rating','API param'],
          ['Test Cards Grid','2-col mobile, 3-col desktop; each card = thumbnail, title, tests count, price, rating, provider','Tests API'],
          ['Pagination','20 per page, infinite scroll on mobile','Offset-based'],
          ['Empty State','If no tests: "Be the first provider! Upload your SSC CGL tests."','Conditional render'],
        ],
        [2200, 4880, 3000]
      ),
      spacer(1),

      h3('Screen 3: Test Detail Page (e.g., /test/ssc-cgl-full-mock-1)'),
      tableWithHeader(
        ['Section','Content'],
        [
          ['Header','Test title | Exam badge | Provider name+logo | Rating (stars + count) | Share button'],
          ['Price Block (sticky CTA)','Price: \u20B9299 | "Buy Now" button | "Try 1 Free Test" link | Validity: Lifetime access'],
          ['Test Info Bar','No. of tests in series | Total questions | Total marks | Duration | Language'],
          ['About Section','Rich text description from provider'],
          ['What\'s Included','Bullet list: Full-length mocks, Previous year papers, Sectional tests, Detailed solutions'],
          ['Exam Pattern Table','Section | Questions | Marks | Duration (per exam type)'],
          ['Provider Card','Provider name, city, total tests published, avg rating, "View all tests" link'],
          ['Reviews Section','Star breakdown bar chart + individual student reviews (Name, Date, Stars, Comment)'],
          ['Related Tests','Horizontal carousel of similar exam tests'],
        ],
        [2400, 7680]
      ),
      spacer(1),

      h3('Screen 4: Checkout (2 Steps)'),
      tableWithHeader(
        ['Step','Elements'],
        [
          ['Step 1 \u2014 Review Order','Test title | Price breakdown (Base + GST + Booking fee) | Promo code field | Grand Total | "Proceed to Pay" button'],
          ['Step 2 \u2014 Payment','Razorpay payment modal opens (UPI / Card / Net Banking / Wallet) | On success \u2192 redirect to My Tests | On failure \u2192 retry screen'],
          ['Order Confirmation','Booking ID | Test name | "Start Test Now" button | "View My Tests" link | Email confirmation sent'],
        ],
        [2400, 7680]
      ),
      spacer(1),

      h3('Screen 5: Test Engine (Timed Exam)'),
      tableWithHeader(
        ['Element','Specification'],
        [
          ['Header Bar','Test name | Section tabs | Timer (countdown MM:SS, turns red at <5 min) | Submit button'],
          ['Question Panel','Question number | Question text (supports LaTeX via KaTeX) | 4 options (A/B/C/D) for MCQ; text input for Numerical'],
          ['Navigation Panel','Grid of question numbers; color codes: Answered (Green), Marked (Orange), Not visited (Grey), Skipped (Red border)'],
          ['Question Actions','Mark for Review | Clear Response | Save & Next'],
          ['Section Switching','Click section tab \u2192 prompt if switching mid-section (optional per exam config)'],
          ['Auto Submit','Timer hits 00:00 \u2192 auto-submit with confirmation toast'],
          ['Anti-Cheat (Basic)','Tab-switch detection: warn on 1st switch, auto-submit after 3rd switch'],
        ],
        [2600, 7480]
      ),
      spacer(1),

      h3('Screen 6: Result Page'),
      tableWithHeader(
        ['Section','Content'],
        [
          ['Score Summary','Total Score | Max Score | Percentile | All-India Rank (among test takers) | Time taken'],
          ['Section Breakdown','Table: Section | Attempted | Correct | Wrong | Score | Accuracy %'],
          ['Performance Badges','Fast Solver | High Accuracy | Top 10% etc. (gamification for engagement)'],
          ['Answer Key','Q-by-Q: Your answer | Correct answer | Status (Correct/Wrong/Skipped) | Marks | Solution text'],
          ['Rank Trend','If retaken: previous rank vs current rank (line chart)'],
          ['CTA','"Retake Test" | "Buy Full Series" | "Share Score" (Twitter/WhatsApp)'],
        ],
        [2600, 7480]
      ),
      spacer(1),

      h3('Screen 7: My Tests (Student Dashboard)'),
      tableWithHeader(
        ['Tab','Content'],
        [
          ['Purchased Tests','Cards: Test name | Provider | Purchased date | Tests taken / Total | "Continue" or "Start" button'],
          ['Free Tests','Tests attempted without purchase'],
          ['My Performance','Exam-wise accuracy trend (last 30 days); weakest topics highlight'],
          ['Order History','Booking ID | Test name | Amount paid | Date | Download Invoice'],
        ],
        [2600, 7480]
      ),
      pageBreak(),

      h2('4.2  Provider Portal (New Module in Admin Panel)'),
      h3('Screen 8: Provider Registration'),
      tableWithHeader(
        ['Field','Type','Validation'],
        [
          ['Institute/Name','Text','Required, 3-100 chars'],
          ['Email','Email','Unique, verified via OTP'],
          ['Mobile','Phone','10-digit, OTP verified'],
          ['City / State','Dropdown','Master list'],
          ['PAN Number','Text','Format: ABCDE1234F (KYC)'],
          ['Bank Account Number','Text','Stored encrypted'],
          ['IFSC Code','Text','Validated via Razorpay API'],
          ['Account Holder Name','Text','Must match PAN'],
          ['Short Bio / About','Textarea','Max 500 chars, shown on test detail'],
          ['Accept Provider Agreement','Checkbox','Required to proceed'],
        ],
        [3000, 2500, 4580]
      ),
      spacer(1),

      h3('Screen 9: Provider \u2014 Create Test Series'),
      tableWithHeader(
        ['Field','Type','Notes'],
        [
          ['Series Title','Text','e.g. "SSC CGL Tier I Full Mock 2026"'],
          ['Exam Category','Dropdown','10 exam types from master'],
          ['Series Type','Radio','Full Mock / Sectional / Previous Year / Mini Mock'],
          ['Number of Tests','Number','How many individual tests in this series'],
          ['Description','Rich Text','Shown on test detail page'],
          ['Thumbnail Image','File Upload','JPG/PNG, max 2MB, 800x450px'],
          ['Language','Dropdown','English (for MVP); Hindi coming soon'],
          ['Base Price (INR)','Number','Min \u20B919, Max \u20B91,999 (platform enforced range)'],
          ['Is First Test Free?','Toggle','IsFreePreview flag on first test'],
          ['Upload Questions','File','CSV per test; uses existing CSV import pipeline'],
          ['Submit for Review','Button','Status \u2192 PENDING_REVIEW; admin notified'],
        ],
        [2800, 1800, 5480]
      ),
      spacer(1),

      h3('Screen 10: Provider Dashboard'),
      tableWithHeader(
        ['Widget','Data Shown'],
        [
          ['Revenue Summary','Gross sales (INR) | Platform commission (30%) | Your earnings (70%) | Pending payout | Paid out to date'],
          ['Test Performance Table','Test name | Views | Purchases | Revenue | Avg Rating | Status'],
          ['Sales Chart','Line chart: daily sales for last 30 days'],
          ['Payout History','Date | Amount | Status (Processed/Pending) | Bank ref'],
          ['Pending Actions','Tests in PENDING_REVIEW | Tests REJECTED (with notes) | Low-rated tests (<3 stars)'],
        ],
        [3000, 7080]
      ),
      pageBreak(),

      h2('4.3  Admin Panel Extensions (New Pages)'),
      tableWithHeader(
        ['New Admin Page','Path','Purpose'],
        [
          ['Provider Management','/Admin/Marketplace/Providers','List, verify, suspend providers; view KYC docs'],
          ['Test Review Queue','/Admin/Marketplace/Reviews','Pending tests: preview, approve/reject with notes'],
          ['Commission Ledger','/Admin/Marketplace/Commissions','All transactions: gross, provider cut, platform cut'],
          ['Payout Management','/Admin/Marketplace/Payouts','Initiate bulk payouts to verified providers'],
          ['Storefront CMS','/Admin/Marketplace/CMS','Hero banners, featured test slots, homepage sections'],
          ['Marketplace Dashboard','/Admin/Dashboard (extended)','New KPIs: providers, test listings, marketplace GMV'],
        ],
        [2800, 3200, 4080]
      ),
      pageBreak(),

      // ── SECTION 5: DATA MODEL ───────────────────────────────────────────────
      h1('5.  Data Model \u2014 New Tables (on Existing GridAcademy DB)'),
      infoBox('Principle', 'Reuse all existing tables: users, subjects, topics, difficulty_levels, exam_types, questions, question_options, tests, test_questions. Add new tables only for marketplace-specific data: provider profiles, test listings, purchases, commissions, payouts, reviews.', LIGHT_BLUE, BLUE),
      spacer(1),

      tableWithHeader(
        ['Table Name','Key Columns','Relationship'],
        [
          ['mp_providers','id (UUID), user_id FK, institute_name, pan_number, bank_account_enc, ifsc, city, state, status (PENDING/VERIFIED/SUSPENDED), created_at','1:1 with users table'],
          ['mp_test_series','id (UUID), provider_id FK, exam_type_id FK, title, slug, description, thumbnail_url, series_type, price_inr, is_first_test_free, status (DRAFT/PENDING_REVIEW/PUBLISHED/REJECTED), published_at, review_notes, purchase_count, avg_rating','N:1 provider; 1:N tests'],
          ['mp_series_tests','id (int), series_id FK, test_id FK (existing tests table), sort_order, is_free_preview','Junction: series \u2194 existing tests'],
          ['mp_orders','id (UUID), student_id FK, series_id FK, amount_inr, gst_amount, booking_fee, grand_total, razorpay_order_id, razorpay_payment_id, status (PENDING/PAID/FAILED/REFUNDED), created_at','N:1 student; N:1 series'],
          ['mp_entitlements','id (UUID), student_id FK, series_id FK, order_id FK, granted_at, expires_at (NULL=lifetime), is_active','Access control: can student take test?'],
          ['mp_commissions','id (UUID), order_id FK, provider_id FK, gross_amount, platform_pct, platform_amount, provider_pct, provider_amount, status (PENDING/PROCESSED), created_at','1:1 with mp_orders'],
          ['mp_payouts','id (UUID), provider_id FK, amount, razorpay_transfer_id, status (INITIATED/SUCCESS/FAILED), initiated_at, completed_at','N:1 provider; aggregates commissions'],
          ['mp_reviews','id (UUID), student_id FK, series_id FK, rating (1-5), comment, created_at, is_visible','N:1 series; post-completion only'],
          ['mp_cms_banners','id (int), title, image_url, link_url, position, is_active, sort_order, valid_from, valid_to','Admin-managed homepage banners'],
          ['mp_promo_codes','id (int), code, discount_type (FLAT/PCT), discount_value, min_order, max_discount, usage_limit, used_count, valid_from, valid_to, is_active','Applied at checkout'],
        ],
        [2400, 4800, 2880]
      ),
      pageBreak(),

      // ── SECTION 6: API ADDITIONS ────────────────────────────────────────────
      h1('6.  New API Endpoints Required'),
      tableWithHeader(
        ['Method','Endpoint','Auth','Purpose'],
        [
          ['POST','/api/auth/send-otp','Public','Send OTP to mobile/email'],
          ['POST','/api/auth/verify-otp','Public','Verify OTP + issue JWT'],
          ['GET','/api/storefront/home','Public','Homepage data (banners, sections)'],
          ['GET','/api/storefront/exams','Public','List 10 exam categories'],
          ['GET','/api/storefront/tests?examType=&sort=&page=','Public','Browse/filter test series'],
          ['GET','/api/storefront/tests/{slug}','Public','Test detail + reviews'],
          ['POST','/api/orders/create','Student JWT','Create Razorpay order'],
          ['POST','/api/orders/verify-payment','Student JWT','Verify Razorpay webhook + grant entitlement'],
          ['GET','/api/student/tests','Student JWT','My purchased tests'],
          ['GET','/api/student/entitlement/{seriesId}','Student JWT','Check if student can take test'],
          ['POST','/api/provider/register','Public','Provider sign-up + KYC data'],
          ['GET','/api/provider/dashboard','Provider JWT','Revenue + test stats'],
          ['GET','/api/provider/series','Provider JWT','Provider\'s own test series list'],
          ['POST','/api/provider/series','Provider JWT','Create new test series'],
          ['PUT','/api/provider/series/{id}','Provider JWT','Update test series'],
          ['POST','/api/provider/series/{id}/submit-review','Provider JWT','Submit series for admin review'],
          ['POST','/api/provider/series/{id}/upload-csv','Provider JWT','Upload questions CSV (reuses existing ImportService)'],
          ['GET','/api/admin/marketplace/reviews','Admin JWT','Pending review queue'],
          ['POST','/api/admin/marketplace/reviews/{id}/approve','Admin JWT','Approve series \u2192 publish'],
          ['POST','/api/admin/marketplace/reviews/{id}/reject','Admin JWT','Reject with notes'],
          ['GET','/api/admin/marketplace/commissions','Admin JWT','All commission records'],
          ['POST','/api/admin/marketplace/payouts/initiate','Admin JWT','Bulk payout to providers'],
          ['GET','/api/admin/marketplace/dashboard','Admin JWT','Marketplace KPIs'],
        ],
        [700, 3800, 1400, 4180]
      ),
      pageBreak(),

      // ── SECTION 7: WHAT IS REUSED ───────────────────────────────────────────
      h1('7.  Existing GridAcademy Code \u2014 Reuse vs New Build'),
      tableWithHeader(
        ['Module / Feature','Status','What to Do'],
        [
          ['ASP.NET Core 8 Web API + Razor Pages','REUSE \u2705','Base framework \u2014 no change'],
          ['PostgreSQL + EF Core + Migrations','REUSE \u2705','Add new migration for mp_* tables'],
          ['JWT Authentication (Admin + API)','REUSE \u2705','Extend with Student + Provider roles'],
          ['User entity + UserService','EXTEND \u26A0','Add roles: Student, Provider to existing role system'],
          ['Question entity (MCQ + Numerical)','REUSE \u2705','No change to question schema'],
          ['Test / Assessment entity','REUSE \u2705','Tests become the units inside mp_series_tests'],
          ['CSV / Excel / PDF Import','REUSE \u2705','Providers use existing ImportController'],
          ['ExamType master table','REUSE \u2705','Already has SSC/RRB/Banking etc.'],
          ['Subject / Topic / Difficulty masters','REUSE \u2705','Used for question tagging in tests'],
          ['Hangfire background jobs','REUSE \u2705','Add: SendOrderConfirmationEmail, ReleasePendingOrder'],
          ['FluentValidation','REUSE \u2705','Add validators for new DTOs'],
          ['ExceptionMiddleware','REUSE \u2705','No change'],
          ['Admin Panel (Razor Pages)','EXTEND \u26A0','Add 6 new Marketplace admin pages (Section 4.3)'],
          ['Bootstrap 5 + admin.css','REUSE \u2705','Extend sidebar with Marketplace section'],
          ['Test Engine (exam UI)','REUSE \u2705','Existing student exam pages \u2014 hook entitlement check'],
          ['Result / Analytics pages','EXTEND \u26A0','Add leaderboard + percentile + badge display'],
          ['Student storefront (homepage + detail)','NEW \u274C','New public-facing Razor Pages or separate frontend'],
          ['Razorpay Payment','NEW \u274C','New PaymentService + webhook endpoint'],
          ['Provider Portal (dashboard + upload)','NEW \u274C','New Provider section in admin panel (restricted role)'],
          ['Commission + Payout Engine','NEW \u274C','New MarketplaceService'],
          ['OTP-based Login','NEW \u274C','New OtpService (MSG91 integration)'],
          ['Star Rating + Reviews','NEW \u274C','New ReviewService + UI on test detail'],
          ['Storefront CMS (banners)','NEW \u274C','New CMS admin page + API'],
        ],
        [3200, 1800, 5080]
      ),
      pageBreak(),

      // ── SECTION 8: SPRINT PLAN ──────────────────────────────────────────────
      h1('8.  Sprint Plan (12-Week MVP Build)'),
      infoBox('Approach', 'All development on existing GridAcademy ASP.NET Core 8 codebase. New marketplace features added as a new module (mp_* tables, Marketplace services/controllers, Provider portal pages, Student storefront pages). Zero changes to existing exam/content module \u2014 additive only.', LIGHT_GREEN, GREEN),
      spacer(1),
      tableWithHeader(
        ['Sprint','Weeks','Focus','Key Deliverables'],
        [
          ['Sprint 1','1\u20132','Foundation','Add Student + Provider roles; OTP auth; mp_providers table; Provider registration flow; DB migration (mp_* tables)'],
          ['Sprint 2','3\u20134','Storefront','Homepage (public), Exam category pages, Test detail page, Test card component; CMS banner admin page; Storefront API endpoints'],
          ['Sprint 3','5\u20136','Payment & Entitlement','Razorpay integration; mp_orders table; Entitlement grant on payment; Order confirmation email (Hangfire); Promo code engine'],
          ['Sprint 4','7\u20138','Provider Portal','Provider dashboard; Test series create/edit; CSV upload flow (reuse ImportService); Submit for review; Review queue in admin; Approve/Reject flow'],
          ['Sprint 5','9\u201310','Test Taking & Results','Entitlement check before test start; Leaderboard (per-test rank); Enhanced result page (percentile, badges, trend chart); Reviews + rating submit'],
          ['Sprint 6','11\u201312','Commission, Payouts & Launch Prep','Commission calculation on order payment; Admin commission ledger; Payout initiation (Razorpay Transfer API); Admin marketplace dashboard KPIs; Mobile-responsive QA pass; End-to-end smoke test; UAT'],
        ],
        [900, 900, 2000, 6280]
      ),
      spacer(1),

      h2('8.1  Technical Debt / Cleanup (Parallel)'),
      bullet('Add SSL / HTTPS enforcement in production middleware'),
      bullet('Environment-based config: appsettings.Development / Staging / Production (already supported by ASP.NET Core)'),
      bullet('Set up GitHub Actions CI pipeline: build + test on every PR'),
      bullet('Seed demo provider + 10 free tests for launch readiness'),
      pageBreak(),

      // ── SECTION 9: NFR ──────────────────────────────────────────────────────
      h1('9.  Non-Functional Requirements'),
      tableWithHeader(
        ['NFR','Target','How to Achieve'],
        [
          ['Performance','Homepage < 1.5s; API < 300ms (P95)','Redis response caching for storefront APIs; PostgreSQL indexes on slug, exam_type_id, status'],
          ['Concurrent Test-Takers','1,000 simultaneous test sessions at MVP launch','Existing test engine is stateless; PostgreSQL handles load; scale App Service tier if needed'],
          ['Mobile Responsiveness','100% on Android Chrome (primary browser of target users)','Bootstrap 5 grid throughout; test on 360px width minimum'],
          ['Uptime','99.5% target for MVP (< 44 hrs downtime/year)','Azure App Service (Always On); Azure PostgreSQL with auto-backup'],
          ['Data Security','No plain-text PAN / bank account in DB','Encrypt bank_account_number field with AES-256; PAN stored hashed'],
          ['Payment Security','PCI-DSS: no card data stored','Razorpay handles card data; we store only order_id + payment_id'],
          ['SEO (Storefront)','Test detail pages indexable by Google','Server-side rendered Razor Pages for /exams/* and /test/* routes (already SSR)'],
          ['GDPR/DPDPA','Consent on registration; delete account on request','Consent checkbox + timestamp stored; soft-delete user on request'],
          ['Accessibility','WCAG 2.1 AA \u2014 keyboard nav + screen reader','Bootstrap\'s ARIA attributes; alt text on all images; sufficient color contrast'],
          ['Audit Log','All admin actions logged','Extend existing AuditLog table; log provider approval/rejection + payout actions'],
        ],
        [2200, 2200, 5680]
      ),
      pageBreak(),

      // ── SECTION 10: OPEN QUESTIONS ──────────────────────────────────────────
      h1('10.  Open Questions & Decisions Required Before Sprint 1'),
      tableWithHeader(
        ['#','Question','Owner','Deadline'],
        [
          ['1','Razorpay merchant account setup \u2014 KYC submitted?','Founder','Before Sprint 3'],
          ['2','MSG91 account for OTP SMS \u2014 registered?','Tech Lead','Before Sprint 1'],
          ['3','SendGrid account for emails \u2014 domain verified?','Tech Lead','Before Sprint 1'],
          ['4','Provider Agreement legal document \u2014 drafted by lawyer?','Legal / CA','Before Sprint 4 (pilot providers)'],
          ['5','GST registration for GridAcademy entity \u2014 obtained?','CA','Before first paid transaction (Sprint 3)'],
          ['6','Azure / cloud hosting plan \u2014 subscription active?','DevOps','Before Sprint 2 (staging)'],
          ['7','Domain name gridacademy.in / .com \u2014 registered?','Founder','Immediate'],
          ['8','Minimum viable content \u2014 how many tests before launch?','Product','Target: 50 tests across 5 exams before public launch'],
          ['9','Pilot provider onboarding \u2014 5 providers ready to upload?','Business Dev','Parallel to Sprint 1-2'],
          ['10','Commission payout cycle \u2014 monthly or fortnightly?','Finance / Legal','Before Sprint 6'],
        ],
        [600, 4200, 2400, 2880]
      ),
      spacer(2),

      // ── SIGN OFF ────────────────────────────────────────────────────────────
      h1('11.  Document Sign-Off'),
      tableWithHeader(
        ['Role','Name','Signature','Date'],
        [
          ['Product Owner','','',''],
          ['Tech Lead','','',''],
          ['Business Lead','','',''],
          ['Operations Lead','','',''],
        ],
        [2400, 2400, 2400, 2880]
      ),
      spacer(2),
      new Paragraph({ alignment: AlignmentType.CENTER, children: [
        new TextRun({ text: '\u2014 End of Product Specification Document v1.0 \u2014', color: '888888', size: 18, font: 'Arial', italics: true })
      ]}),
      new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 60 }, children: [
        new TextRun({ text: 'GridAcademy  |  Confidential  |  March 2026', color: 'AAAAAA', size: 16, font: 'Arial' })
      ]}),
    ]
  }]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync('GridAcademy_PSD_v1.0.docx', buffer);
  console.log('SUCCESS: GridAcademy_PSD_v1.0.docx created');
}).catch(err => {
  console.error('ERROR:', err.message);
  process.exit(1);
});
