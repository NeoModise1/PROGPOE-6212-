
Use RaceDays


CREATE TABLE dbo.USERS
(
    UserID INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_USERS PRIMARY KEY (UserID),
    CONSTRAINT UQ_USERS_Email UNIQUE (Email)
);


CREATE TABLE dbo.EVENTS
(
    EventID INT IDENTITY(1,1) NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    EventDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    Venue NVARCHAR(200) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Province NVARCHAR(100) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Scheduled',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_EVENTS PRIMARY KEY (EventID)
);

CREATE TABLE dbo.CATEGORIES
(
    CategoryID INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    DistanceKM DECIMAL(6,2) NOT NULL,
    ActivityType NVARCHAR(30) NOT NULL,
    Description NVARCHAR(500) NOT NULL,

    CONSTRAINT PK_CATEGORIES PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_CATEGORIES_Name_Distance
        UNIQUE (CategoryName, DistanceKM)
);


CREATE TABLE dbo.USER_EVENTS
(
    UserEventID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    EventID INT NOT NULL,
    RegistrationDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    RaceNumber NVARCHAR(30) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Registered',

    CONSTRAINT PK_USER_EVENTS PRIMARY KEY (UserEventID),

    CONSTRAINT UQ_USER_EVENTS_User_Event
        UNIQUE (UserID, EventID),

    CONSTRAINT FK_USER_EVENTS_User
        FOREIGN KEY (UserID) REFERENCES dbo.USERS(UserID),

    CONSTRAINT FK_USER_EVENTS_Event
        FOREIGN KEY (EventID) REFERENCES dbo.EVENTS(EventID)
);


CREATE TABLE dbo.EVENT_CATEGORIES
(
    EventCategoryID INT IDENTITY(1,1) NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    MaximumParticipants INT NOT NULL,

    CONSTRAINT PK_EVENT_CATEGORIES
        PRIMARY KEY (EventCategoryID),

    CONSTRAINT UQ_EVENT_CATEGORIES_Event_Category
        UNIQUE (EventID, CategoryID),

    CONSTRAINT FK_EVENT_CATEGORIES_Event
        FOREIGN KEY (EventID) REFERENCES dbo.EVENTS(EventID),

    CONSTRAINT FK_EVENT_CATEGORIES_Category
        FOREIGN KEY (CategoryID) REFERENCES dbo.CATEGORIES(CategoryID)
);


CREATE TABLE dbo.ENROLLMENTS
(
    EnrollmentID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    EventCategoryID INT NOT NULL,
    EnrollmentDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Registered',

    CONSTRAINT PK_ENROLLMENTS
        PRIMARY KEY (EnrollmentID),

    CONSTRAINT UQ_ENROLLMENTS_User_EventCategory
        UNIQUE (UserID, EventCategoryID),

    CONSTRAINT FK_ENROLLMENTS_User
        FOREIGN KEY (UserID) REFERENCES dbo.USERS(UserID),

    CONSTRAINT FK_ENROLLMENTS_EventCategory
        FOREIGN KEY (EventCategoryID)
        REFERENCES dbo.EVENT_CATEGORIES(EventCategoryID)
);

CREATE TABLE dbo.RESULTS
(
    ResultID INT IDENTITY(1,1) NOT NULL,
    EnrollmentID INT NOT NULL,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL,
    AveragePace DECIMAL(6,2) NOT NULL,
    ResultStatus NVARCHAR(30) NOT NULL DEFAULT 'Completed',
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_RESULTS PRIMARY KEY (ResultID),

    CONSTRAINT UQ_RESULTS_Enrollment
        UNIQUE (EnrollmentID),

    CONSTRAINT FK_RESULTS_Enrollment
        FOREIGN KEY (EnrollmentID)
        REFERENCES dbo.ENROLLMENTS(EnrollmentID)
);



CREATE TABLE dbo.ROUTES
(
    RouteID INT IDENTITY(1,1) NOT NULL,
    EventID INT NOT NULL,
    RouteName NVARCHAR(150) NOT NULL,
    DistanceKM DECIMAL(6,2) NOT NULL,
    ElevationGain DECIMAL(6,2) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    MapURL NVARCHAR(500) NOT NULL,

    CONSTRAINT PK_ROUTES PRIMARY KEY (RouteID),

    CONSTRAINT FK_ROUTES_Event
        FOREIGN KEY (EventID) REFERENCES dbo.EVENTS(EventID)
);

CREATE TABLE dbo.WEATHER_INFORMATION
(
    WeatherID INT IDENTITY(1,1) NOT NULL,
    EventID INT NOT NULL,
    Temperature DECIMAL(5,2) NOT NULL,
    WeatherCondition NVARCHAR(100) NOT NULL,
    WindSpeed DECIMAL(6,2) NOT NULL,
    Humidity DECIMAL(5,2) NOT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_WEATHER_INFORMATION
        PRIMARY KEY (WeatherID),

    CONSTRAINT FK_WEATHER_Event
        FOREIGN KEY (EventID) REFERENCES dbo.EVENTS(EventID)
);


INSERT INTO dbo.USERS
(
    FirstName,
    LastName,
    Email,
    PasswordHash,
    PhoneNumber,
    Role
)
VALUES
(
    'Thabo',
    'Mokoena',
    'thabo.mokoena@raceday.co.za',
    'HASH_ORGANISER_001',
    '0825550101',
    'Organiser'
),
(
    'Lerato',
    'Dlamini',
    'lerato.dlamini@raceday.co.za',
    'HASH_ORGANISER_002',
    '0835550102',
    'Organiser'
),
(
    'Sipho',
    'Ndlovu',
    'sipho.ndlovu@example.com',
    'HASH_PARTICIPANT_001',
    '0845550103',
    'Participant'
),
(
    'Aisha',
    'Naidoo',
    'aisha.naidoo@example.com',
    'HASH_PARTICIPANT_002',
    '0855550104',
    'Participant'
);


INSERT INTO dbo.EVENTS
(
    EventName,
    Description,
    EventDate,
    StartTime,
    Venue,
    City,
    Province,
    Status
)
VALUES
(
    'Cape Town Coastal Run',
    'A scenic road race along the Cape Town coastline.',
    '2026-10-18',
    '07:00:00',
    'Green Point Athletics Stadium',
    'Cape Town',
    'Western Cape',
    'Scheduled'
),
(
    'Johannesburg City Challenge',
    'A city running event featuring short and medium-distance races.',
    '2026-11-08',
    '06:30:00',
    'Mary Fitzgerald Square',
    'Johannesburg',
    'Gauteng',
    'Scheduled'
),
(
    'Durban Summer Run',
    'A fast-paced running event along the Durban beachfront.',
    '2026-12-06',
    '06:00:00',
    'Moses Mabhida Stadium',
    'Durban',
    'KwaZulu-Natal',
    'Scheduled'
);
GO

INSERT INTO dbo.CATEGORIES
(
    CategoryName,
    DistanceKM,
    ActivityType,
    Description
)
VALUES
(
    '5 KM Fun Run',
    5.00,
    'Running',
    'A beginner-friendly five kilometre road race.'
),
(
    '10 KM Road Race',
    10.00,
    'Running',
    'A standard ten kilometre competitive road race.'
),
(
    '21 KM Half Marathon',
    21.10,
    'Running',
    'A half-marathon distance race for experienced runners.'
),
(
    '5 KM Walk',
    5.00,
    'Walking',
    'A recreational five kilometre walking category.'
);
GO

INSERT INTO dbo.EVENT_CATEGORIES
(
    EventID,
    CategoryID,
    EntryFee,
    MaximumParticipants
)
VALUES

-- Cape Town Coastal Run
(1, 1, 120.00, 500),
(1, 2, 180.00, 750),
(1, 3, 300.00, 500),

-- Johannesburg City Challenge
(2, 1, 100.00, 600),
(2, 2, 160.00, 800),
(2, 3, 280.00, 600),

-- Durban Summer Run
(3, 1, 110.00, 500),
(3, 2, 170.00, 700),
(3, 4, 90.00, 300);

GO

INSERT INTO dbo.USER_EVENTS
(
    UserID,
    EventID,
    RegistrationDate,
    RaceNumber,
    Status
)
VALUES
(
    3,
    1,
    '2026-08-20 09:15:00',
    'CT-001',
    'Registered'
),
(
    4,
    1,
    '2026-08-21 10:30:00',
    'CT-002',
    'Registered'
),
(
    3,
    2,
    '2026-08-25 08:45:00',
    'JHB-001',
    'Registered'
),
(
    4,
    3,
    '2026-08-28 11:00:00',
    'DBN-001',
    'Registered'
);
GO

INSERT INTO dbo.ENROLLMENTS
(
    UserID,
    EventCategoryID,
    EnrollmentDate,
    Status
)
VALUES
(
    3,
    1,
    '2026-08-20 09:20:00',
    'Registered'
),
(
    4,
    2,
    '2026-08-21 10:35:00',
    'Registered'
),
(
    3,
    5,
    '2026-08-25 08:50:00',
    'Registered'
),
(
    4,
    7,
    '2026-08-28 11:05:00',
    'Registered'
);
GO

INSERT INTO dbo.RESULTS
(
    EnrollmentID,
    FinishTime,
    Position,
    AveragePace,
    ResultStatus
)
VALUES
(
    1,
    '00:29:45',
    18,
    5.95,
    'Completed'
),
(
    2,
    '00:51:20',
    11,
    5.13,
    'Completed'
);
GO

INSERT INTO dbo.ROUTES
(
    EventID,
    RouteName,
    DistanceKM,
    ElevationGain,
    Description,
    MapURL
)
VALUES
(
    1,
    'Atlantic Seaboard Loop',
    10.00,
    85.00,
    'Coastal route through Green Point and along the Atlantic Seaboard.',
    'https://maps.example.com/raceday/cape-town-coastal'
),
(
    1,
    'Table Bay Half Marathon Route',
    21.10,
    145.00,
    'Half-marathon route with coastal views and moderate elevation.',
    'https://maps.example.com/raceday/cape-town-half'
),
(
    2,
    'Johannesburg CBD Circuit',
    10.00,
    120.00,
    'Urban circuit through central Johannesburg.',
    'https://maps.example.com/raceday/johannesburg-city'
),
(
    3,
    'Durban Beachfront Route',
    10.00,
    55.00,
    'Flat beachfront route with a fast finish.',
    'https://maps.example.com/raceday/durban-summer'
);
GO


INSERT INTO dbo.WEATHER_INFORMATION
(
    EventID,
    Temperature,
    WeatherCondition,
    WindSpeed,
    Humidity,
    RecordedAt
)
VALUES
(
    1,
    17.50,
    'Partly Cloudy',
    12.50,
    68.00,
    '2026-10-18 06:00:00'
),
(
    1,
    19.00,
    'Sunny',
    14.20,
    62.00,
    '2026-10-18 08:00:00'
),
(
    2,
    15.00,
    'Clear',
    8.00,
    55.00,
    '2026-11-08 05:45:00'
),
(
    3,
    22.50,
    'Sunny',
    6.50,
    70.00,
    '2026-12-06 05:30:00'
);
GO
