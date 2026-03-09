SELECT * FROM Users;

  with Numbers as (
	select top (100000)
		ROW_NUMBER() over (order by (select null)) as n
	from sys.all_objects a
	cross join sys.all_objects b
  )
  insert into Users(
	UserId,
    FullName,
    Email,
    PhoneNumber,
    DateOfBirth,
    Gender,
    Address,
    Avatar,
    StudentID,
    Major,
    JoinDate,
    Status,
    PasswordHash,
    CreatedAt,
    UpdatedAt
  )

  Select 
	NEWID(),
    N'Test User ' + CAST(n AS NVARCHAR),
    'testuser' + CAST(n AS NVARCHAR) + '@mail.com',
    '09' + RIGHT('00000000' + CAST(n AS VARCHAR), 8),
    DATEADD(DAY, - (n % 10000), GETDATE()),
    CASE WHEN n % 2 = 0 THEN 'Male' ELSE 'Female' END,
    N'Address ' + CAST(n AS NVARCHAR),
    '/images/default-avatar.png',
    'SV' + RIGHT('000000' + CAST(n AS VARCHAR), 6),
    CASE 
        WHEN n % 3 = 0 THEN 'Information Technology'
        WHEN n % 3 = 1 THEN 'Business Administration'
        ELSE 'Graphic Design'
    END,
    DATEADD(DAY, - (n % 365), GETDATE()),
    CASE 
        WHEN n % 5 = 0 THEN 'Pending'
        ELSE 'Active'
    END,
    '$2a$11$FakeHashForLoadTestingOnly',
    GETDATE(),
    GETDATE()
From Numbers;


