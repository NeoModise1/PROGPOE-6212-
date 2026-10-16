/* =====================================================================
   RaceDay - SQL Server schema + sample data
   Matches the RaceDay ERD (Part 1) and the EF Core Code-First model used
   by the Part 2 API (src/RaceDay.Api).

   Run this in SSMS / Visual Studio SQL Server Object Explorer against
   (localdb)\MSSQLLocalDB. It is re-runnable: it drops and recreates the
   RaceDayDb tables.

   Sample logins (all four users): password  Password123!
     thabo.mokoena@raceday.co.za   (Organiser)
     lerato.dlamini@raceday.co.za  (Organiser)
     sipho.ndlovu@example.com      (Participant)
     aisha.naidoo@example.com      (Participant)
   ===================================================================== */

IF DB_ID(N'RaceDayDb') IS NULL
    CREATE DATABASE RaceDayDb;
GO

USE RaceDayDb;
GO

/* ---------- Drop existing tables (child tables first) ---------- */
DROP TABLE IF EXISTS dbo.RESULTS;
DROP TABLE IF EXISTS dbo.ENROLLMENTS;
DROP TABLE IF EXISTS dbo.USER_EVENTS;
DROP TABLE IF EXISTS dbo.WEATHER_INFORMATION;
DROP TABLE IF EXISTS dbo.ROUTES;
DROP TABLE IF EXISTS dbo.EVENT_CATEGORIES;
DROP TABLE IF EXISTS dbo.CATEGORIES;
DROP TABLE IF EXISTS dbo.EVENTS;
DROP TABLE IF EXISTS dbo.PARTICIPANTS;
DROP TABLE IF EXISTS dbo.ORGANISERS;
DROP TABLE IF EXISTS dbo.USERS;
GO

/* ---------- USERS ---------- */
CREATE TABLE dbo.USERS
(
    UserID       INT IDENTITY(1,1) NOT NULL,
    FirstName    NVARCHAR(50)  NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber  NVARCHAR(20)  NULL,
    Role         NVARCHAR(20)  NOT NULL,
    CreatedAt    DATETIME2     NOT NULL CONSTRAINT DF_USERS_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_USERS PRIMARY KEY (UserID),
    CONSTRAINT UQ_USERS_Email UNIQUE (Email),
    CONSTRAINT CK_USERS_Role CHECK (Role IN (N'Organiser', N'Participant'))
);
GO

/* ---------- ORGANISERS (a user registered as an Organiser) ---------- */
CREATE TABLE dbo.ORGANISERS
(
    OrganiserID INT IDENTITY(1,1) NOT NULL,
    UserID      INT       NOT NULL,
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_ORGANISERS_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ORGANISERS PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_ORGANISERS_User UNIQUE (UserID),
    CONSTRAINT FK_ORGANISERS_User FOREIGN KEY (UserID)
        REFERENCES dbo.USERS(UserID) ON DELETE CASCADE
);
GO

/* ---------- PARTICIPANTS (a user registered as a Participant) ---------- */
CREATE TABLE dbo.PARTICIPANTS
(
    ParticipantID INT IDENTITY(1,1) NOT NULL,
    UserID        INT       NOT NULL,
    CreatedAt     DATETIME2 NOT NULL CONSTRAINT DF_PARTICIPANTS_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PARTICIPANTS PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_PARTICIPANTS_User UNIQUE (UserID),
    CONSTRAINT FK_PARTICIPANTS_User FOREIGN KEY (UserID)
        REFERENCES dbo.USERS(UserID) ON DELETE CASCADE
);
GO

/* ---------- EVENTS (organised by one Organiser) ---------- */
CREATE TABLE dbo.EVENTS
(
    EventID     INT IDENTITY(1,1) NOT NULL,
    OrganiserID INT            NOT NULL,
    EventName   NVARCHAR(150)  NOT NULL,
    Description NVARCHAR(1000) NULL,
    EventDate   DATE           NOT NULL,
    StartTime   TIME           NOT NULL,
    Venue       NVARCHAR(200)  NOT NULL,
    City        NVARCHAR(100)  NOT NULL,
    Province    NVARCHAR(100)  NOT NULL,
    Status      NVARCHAR(30)   NOT NULL CONSTRAINT DF_EVENTS_Status DEFAULT N'Scheduled',
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_EVENTS_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_EVENTS PRIMARY KEY (EventID),
    CONSTRAINT FK_EVENTS_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES dbo.ORGANISERS(OrganiserID) ON DELETE NO ACTION
);
GO

/* ---------- CATEGORIES (master list) ---------- */
CREATE TABLE dbo.CATEGORIES
(
    CategoryID   INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    DistanceKM   DECIMAL(6,2)  NOT NULL,
    ActivityType NVARCHAR(30)  NOT NULL,
    Description  NVARCHAR(500) NULL,

    CONSTRAINT PK_CATEGORIES PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_CATEGORIES_Name_Distance UNIQUE (CategoryName, DistanceKM),
    CONSTRAINT CK_CATEGORIES_ActivityType CHECK (ActivityType IN (N'Run', N'Walk', N'Cycle'))
);
GO

/* ---------- EVENT_CATEGORIES (an event offers many categories) ---------- */
CREATE TABLE dbo.EVENT_CATEGORIES
(
    EventCategoryID     INT IDENTITY(1,1) NOT NULL,
    EventID             INT           NOT NULL,
    CategoryID          INT           NOT NULL,
    EntryFee            DECIMAL(10,2) NOT NULL,
    MaximumParticipants INT           NOT NULL,

    CONSTRAINT PK_EVENT_CATEGORIES PRIMARY KEY (EventCategoryID),
    CONSTRAINT UQ_EVENT_CATEGORIES_Event_Category UNIQUE (EventID, CategoryID),
    CONSTRAINT FK_EVENT_CATEGORIES_Event FOREIGN KEY (EventID)
        REFERENCES dbo.EVENTS(EventID) ON DELETE CASCADE,
    CONSTRAINT FK_EVENT_CATEGORIES_Category FOREIGN KEY (CategoryID)
        REFERENCES dbo.CATEGORIES(CategoryID) ON DELETE NO ACTION
);
GO

/* ---------- ENROLLMENTS (participant enrols in an event category) ---------- */
CREATE TABLE dbo.ENROLLMENTS
(
    EnrollmentID    INT IDENTITY(1,1) NOT NULL,
    ParticipantID   INT          NOT NULL,
    EventCategoryID INT          NOT NULL,
    EnrollmentDate  DATETIME2    NOT NULL CONSTRAINT DF_ENROLLMENTS_Date DEFAULT SYSUTCDATETIME(),
    Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_ENROLLMENTS_Status DEFAULT N'Registered',

    CONSTRAINT PK_ENROLLMENTS PRIMARY KEY (EnrollmentID),
    CONSTRAINT UQ_ENROLLMENTS_Participant_EventCategory UNIQUE (ParticipantID, EventCategoryID),
    CONSTRAINT FK_ENROLLMENTS_Participant FOREIGN KEY (ParticipantID)
        REFERENCES dbo.PARTICIPANTS(ParticipantID) ON DELETE NO ACTION,
    CONSTRAINT FK_ENROLLMENTS_EventCategory FOREIGN KEY (EventCategoryID)
        REFERENCES dbo.EVENT_CATEGORIES(EventCategoryID) ON DELETE CASCADE
);
GO

/* ---------- USER_EVENTS (participant registered for an event) ---------- */
CREATE TABLE dbo.USER_EVENTS
(
    UserEventID      INT IDENTITY(1,1) NOT NULL,
    ParticipantID    INT          NOT NULL,
    EventID          INT          NOT NULL,
    RegistrationDate DATETIME2    NOT NULL CONSTRAINT DF_USER_EVENTS_Date DEFAULT SYSUTCDATETIME(),
    RaceNumber       NVARCHAR(30) NULL,
    Status           NVARCHAR(30) NOT NULL CONSTRAINT DF_USER_EVENTS_Status DEFAULT N'Registered',

    CONSTRAINT PK_USER_EVENTS PRIMARY KEY (UserEventID),
    CONSTRAINT UQ_USER_EVENTS_Participant_Event UNIQUE (ParticipantID, EventID),
    CONSTRAINT FK_USER_EVENTS_Participant FOREIGN KEY (ParticipantID)
        REFERENCES dbo.PARTICIPANTS(ParticipantID) ON DELETE NO ACTION,
    CONSTRAINT FK_USER_EVENTS_Event FOREIGN KEY (EventID)
        REFERENCES dbo.EVENTS(EventID) ON DELETE CASCADE
);
GO

/* ---------- RESULTS (0..1 per enrolment) ---------- */
CREATE TABLE dbo.RESULTS
(
    ResultID     INT IDENTITY(1,1) NOT NULL,
    EnrollmentID INT           NOT NULL,
    FinishTime   TIME          NULL,
    Position     INT           NULL,
    AveragePace  DECIMAL(6,2)  NULL,
    ResultStatus NVARCHAR(30)  NOT NULL CONSTRAINT DF_RESULTS_Status DEFAULT N'Completed',
    RecordedAt   DATETIME2     NOT NULL CONSTRAINT DF_RESULTS_RecordedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_RESULTS PRIMARY KEY (ResultID),
    CONSTRAINT UQ_RESULTS_Enrollment UNIQUE (EnrollmentID),
    CONSTRAINT FK_RESULTS_Enrollment FOREIGN KEY (EnrollmentID)
        REFERENCES dbo.ENROLLMENTS(EnrollmentID) ON DELETE CASCADE
);
GO

/* ---------- ROUTES (an event has one or more routes) ---------- */
CREATE TABLE dbo.ROUTES
(
    RouteID       INT IDENTITY(1,1) NOT NULL,
    EventID       INT           NOT NULL,
    RouteName     NVARCHAR(150) NOT NULL,
    DistanceKM    DECIMAL(6,2)  NOT NULL,
    ElevationGain DECIMAL(6,2)  NOT NULL,
    Description   NVARCHAR(500) NULL,
    MapURL        NVARCHAR(500) NULL,

    CONSTRAINT PK_ROUTES PRIMARY KEY (RouteID),
    CONSTRAINT FK_ROUTES_Event FOREIGN KEY (EventID)
        REFERENCES dbo.EVENTS(EventID) ON DELETE CASCADE
);
GO

/* ---------- WEATHER_INFORMATION (an event has many weather records) ---------- */
CREATE TABLE dbo.WEATHER_INFORMATION
(
    WeatherID        INT IDENTITY(1,1) NOT NULL,
    EventID          INT          NOT NULL,
    Temperature      DECIMAL(5,2) NOT NULL,
    WeatherCondition NVARCHAR(100) NOT NULL,
    WindSpeed        DECIMAL(6,2) NOT NULL,
    Humidity         DECIMAL(5,2) NOT NULL,
    RecordedAt       DATETIME2    NOT NULL CONSTRAINT DF_WEATHER_RecordedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_WEATHER_INFORMATION PRIMARY KEY (WeatherID),
    CONSTRAINT FK_WEATHER_Event FOREIGN KEY (EventID)
        REFERENCES dbo.EVENTS(EventID) ON DELETE CASCADE
);
GO

/* =====================================================================
   SAMPLE DATA
   ===================================================================== */

/* Users - PasswordHash values are real hashes of: Password123! */
INSERT INTO dbo.USERS (FirstName, LastName, Email, PasswordHash, PhoneNumber, Role)
VALUES
(N'Thabo',  N'Mokoena', N'thabo.mokoena@raceday.co.za',  N'100000.jZanHpaJs5iaoDfSIxqUYQ==.MdEm/DOHo28zx27r8QlATy25Ljv0aK2tOl84kaBE1QE=', N'0825550101', N'Organiser'),
(N'Lerato', N'Dlamini', N'lerato.dlamini@raceday.co.za', N'100000.UBM1e3QJcf1ojwXcTXT6Vg==.G2mbOjxoH5hS2RQU7YJrGSkHFF6nqHrZwjojRcusQ2A=', N'0835550102', N'Organiser'),
(N'Sipho',  N'Ndlovu',  N'sipho.ndlovu@example.com',     N'100000.6E46Na/zQbcTvkNVjGk2nA==.AeXNHdXZ3L4Qi+wQKCR5u8hsvDzU36st0MF2SQ0/yaI=', N'0845550103', N'Participant'),
(N'Aisha',  N'Naidoo',  N'aisha.naidoo@example.com',     N'100000.m7495MSHxPBXc4GAFFQ42A==.8feWcyrrv7UIVi9DKc4UfJeFuedvXg+pZK7WmK1xUYM=', N'0855550104', N'Participant');
GO

/* Role records: Users 1-2 are Organisers, Users 3-4 are Participants */
INSERT INTO dbo.ORGANISERS (UserID) VALUES (1), (2);
INSERT INTO dbo.PARTICIPANTS (UserID) VALUES (3), (4);
GO

/* Events (OrganiserID 1 = Thabo, 2 = Lerato) */
INSERT INTO dbo.EVENTS (OrganiserID, EventName, Description, EventDate, StartTime, Venue, City, Province, Status)
VALUES
(1, N'Cape Town Coastal Run',        N'A scenic road race along the Cape Town coastline.',                   '2026-10-18', '07:00:00', N'Green Point Athletics Stadium', N'Cape Town',    N'Western Cape',  N'Scheduled'),
(2, N'Johannesburg City Challenge',  N'A city running event featuring short and medium-distance races.',    '2026-11-08', '06:30:00', N'Mary Fitzgerald Square',        N'Johannesburg', N'Gauteng',       N'Scheduled'),
(1, N'Durban Summer Run',            N'A fast-paced running event along the Durban beachfront.',             '2026-12-06', '06:00:00', N'Moses Mabhida Stadium',         N'Durban',       N'KwaZulu-Natal', N'Scheduled');
GO

/* Master category list */
INSERT INTO dbo.CATEGORIES (CategoryName, DistanceKM, ActivityType, Description)
VALUES
(N'5 KM Fun Run',        5.00,  N'Run',   N'A beginner-friendly five kilometre road race.'),
(N'10 KM Road Race',     10.00, N'Run',   N'A standard ten kilometre competitive road race.'),
(N'21 KM Half Marathon', 21.10, N'Run',   N'A half-marathon distance race for experienced runners.'),
(N'5 KM Walk',           5.00,  N'Walk',  N'A recreational five kilometre walking category.'),
(N'20 KM Cycle',         20.00, N'Cycle', N'A twenty kilometre cycle.'),
(N'40 KM Cycle',         40.00, N'Cycle', N'A forty kilometre cycle.');
GO

/* Event categories */
INSERT INTO dbo.EVENT_CATEGORIES (EventID, CategoryID, EntryFee, MaximumParticipants)
VALUES
(1, 1, 120.00, 500), (1, 2, 180.00, 750), (1, 3, 300.00, 500),   -- Cape Town  (EventCategoryID 1-3)
(2, 1, 100.00, 600), (2, 2, 160.00, 800), (2, 3, 280.00, 600),   -- Johannesburg (4-6)
(3, 1, 110.00, 500), (3, 2, 170.00, 700), (3, 4,  90.00, 300);   -- Durban (7-9)
GO

/* Registrations (ParticipantID 1 = Sipho, 2 = Aisha) */
INSERT INTO dbo.USER_EVENTS (ParticipantID, EventID, RegistrationDate, RaceNumber, Status)
VALUES
(1, 1, '2026-08-20 09:15:00', N'CT-001',  N'Registered'),
(2, 1, '2026-08-21 10:30:00', N'CT-002',  N'Registered'),
(1, 2, '2026-08-25 08:45:00', N'JHB-001', N'Registered'),
(2, 3, '2026-08-28 11:00:00', N'DBN-001', N'Registered');
GO

/* Enrolments */
INSERT INTO dbo.ENROLLMENTS (ParticipantID, EventCategoryID, EnrollmentDate, Status)
VALUES
(1, 1, '2026-08-20 09:20:00', N'Registered'),
(2, 2, '2026-08-21 10:35:00', N'Registered'),
(1, 5, '2026-08-25 08:50:00', N'Registered'),
(2, 7, '2026-08-28 11:05:00', N'Registered');
GO

/* Results */
INSERT INTO dbo.RESULTS (EnrollmentID, FinishTime, Position, AveragePace, ResultStatus)
VALUES
(1, '00:29:45', 18, 5.95, N'Completed'),
(2, '00:51:20', 11, 5.13, N'Completed');
GO

/* Routes */
INSERT INTO dbo.ROUTES (EventID, RouteName, DistanceKM, ElevationGain, Description, MapURL)
VALUES
(1, N'Atlantic Seaboard Loop',        10.00,  85.00, N'Coastal route through Green Point and along the Atlantic Seaboard.', N'https://maps.example.com/raceday/cape-town-coastal'),
(1, N'Table Bay Half Marathon Route', 21.10, 145.00, N'Half-marathon route with coastal views and moderate elevation.',      N'https://maps.example.com/raceday/cape-town-half'),
(2, N'Johannesburg CBD Circuit',      10.00, 120.00, N'Urban circuit through central Johannesburg.',                        N'https://maps.example.com/raceday/johannesburg-city'),
(3, N'Durban Beachfront Route',       10.00,  55.00, N'Flat beachfront route with a fast finish.',                          N'https://maps.example.com/raceday/durban-summer');
GO

/* Weather */
INSERT INTO dbo.WEATHER_INFORMATION (EventID, Temperature, WeatherCondition, WindSpeed, Humidity, RecordedAt)
VALUES
(1, 17.50, N'Partly Cloudy', 12.50, 68.00, '2026-10-18 06:00:00'),
(1, 19.00, N'Sunny',         14.20, 62.00, '2026-10-18 08:00:00'),
(2, 15.00, N'Clear',          8.00, 55.00, '2026-11-08 05:45:00'),
(3, 22.50, N'Sunny',          6.50, 70.00, '2026-12-06 05:30:00');
GO
