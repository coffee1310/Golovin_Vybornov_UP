USE PropertyManagement;
GO

-- 1. Buildings Table
CREATE TABLE Buildings (
    building_id INT PRIMARY KEY IDENTITY(1,1),
    address NVARCHAR(255) NOT NULL,
    city NVARCHAR(100) NOT NULL,
    management_start_date DATE,
    floors INT,
    apartments_count INT,
    construction_year INT,
    total_area DECIMAL(10,2)
);
GO

-- 2. Apartments Table
CREATE TABLE Apartments (
    apartment_id INT PRIMARY KEY IDENTITY(1,1),
    building_id INT NOT NULL FOREIGN KEY REFERENCES Buildings(building_id),
    apartment_number INT NOT NULL,
    area DECIMAL(10,2)
);
GO

-- 3. Owners Table
CREATE TABLE Owners (
    owner_id INT PRIMARY KEY IDENTITY(1,1),
    full_name NVARCHAR(255) NOT NULL,
    phone_number NVARCHAR(20),
    passport_data NVARCHAR(100)
);
GO

-- 4. PropertyOwnership Table
CREATE TABLE PropertyOwnership (
    ownership_id INT PRIMARY KEY IDENTITY(1,1),
    owner_id INT NOT NULL FOREIGN KEY REFERENCES Owners(owner_id),
    apartment_id INT NOT NULL FOREIGN KEY REFERENCES Apartments(apartment_id)
);
GO

-- 5. Debts Table
CREATE TABLE Debts (
    debt_id INT PRIMARY KEY IDENTITY(1,1),
    apartment_id INT NOT NULL FOREIGN KEY REFERENCES Apartments(apartment_id),
    report_date DATE NOT NULL,
    water_debt DECIMAL(10,2),
    electricity_debt DECIMAL(10,2)
);
GO

-- 6. Payments Table
CREATE TABLE Payments (
    payment_id INT PRIMARY KEY IDENTITY(1,1),
    apartment_id INT NOT NULL FOREIGN KEY REFERENCES Apartments(apartment_id),
    period_month INT NOT NULL,
    period_year INT NOT NULL,
    amount_charged DECIMAL(10,2) NOT NULL,
    amount_paid DECIMAL(10,2) DEFAULT 0
);
GO

-- 7. Employees Table
CREATE TABLE Employees (
    employee_id INT PRIMARY KEY IDENTITY(1,1),
    full_name NVARCHAR(255) NOT NULL,
    position NVARCHAR(100),
    phone_number NVARCHAR(20),
    email NVARCHAR(100)
);
GO

-- 8. ServiceRequests Table
CREATE TABLE ServiceRequests (
    request_id INT PRIMARY KEY IDENTITY(1,1),
    apartment_id INT NOT NULL FOREIGN KEY REFERENCES Apartments(apartment_id),
    employee_id INT FOREIGN KEY REFERENCES Employees(employee_id),
    created_date DATETIME DEFAULT GETDATE(),
    request_type NVARCHAR(100) NOT NULL,
    description NVARCHAR(1000),
    status NVARCHAR(50) DEFAULT 'Открыта'
);
GO

-- Insert data from Excel files

-- Insert data from Список жилого фонда.xlsx
INSERT INTO Buildings (address, city, management_start_date, floors, apartments_count, construction_year, total_area)
VALUES 
(N'ул. 45 Параллель, 4/2, Ставрополь', N'Ставрополь', '2015-04-22', 9, 52, 2001, 1978.7),
(N'ул. Васильева, 1, Ставрополь', N'Ставрополь', '2015-04-22', 9, 144, 1983, 7950.4),
(N'ул. Доваторцев, 66/2, Ставрополь', N'Ставрополь', '2020-11-01', 9, 102, 1984, 3176.4),
(N'ул. Мира, 236, Ставрополь', N'Ставрополь', '2007-11-04', 10, 62, 1991, 4423.1),
(N'ул. Мира, 272, Ставрополь', N'Ставрополь', '2015-04-22', 9, 88, 2006, 6204.7),
(N'ул. Мира, 278, Ставрополь', N'Ставрополь', '2019-08-01', 10, 40, 2008, 3294.1),
(N'пл. Выставочная, 40, Светлоград', N'Светлоград', '2018-12-01', 5, 70, 1985, 2369.2),
(N'пл. Выставочная, 43, Светлоград', N'Светлоград', '2019-07-01', 5, 68, 1987, 3702.8),
(N'пл. Выставочная, 45, Светлоград', N'Светлоград', '2019-12-01', 4, 68, 1990, 3731.5),
(N'пл. Выставочная, 47, Светлоград', N'Светлоград', '2019-07-01', 5, 68, 1993, 4079.2),
(N'пл. Выставочная, 48, Светлоград', N'Светлоград', '2018-12-01', 5, 68, 1995, 3654.6),
(N'пл. Выставочная, 49, Светлоград', N'Светлоград', '2021-06-01', 5, 60, 1995, 2891.7),
(N'пл. Выставочная, 50, Светлоград', N'Светлоград', '2019-02-01', 5, 60, 1995, 4014.5),
(N'пл. Выставочная, 57, Светлоград', N'Светлоград', '2020-02-01', 3, 36, 2015, 2075.7),
(N'пл. Выставочная, 58, Светлоград', N'Светлоград', '2021-11-01', 5, NULL, 2013, 3124.2),
(N'ул. Бассейная, 82, Светлоград', N'Светлоград', '2021-03-01', 5, 48, 1988, 3255.3),
(N'ул. Красная, 44а, Светлоград', N'Светлоград', '2022-03-01', 5, 60, 1983, 4317.4),
(N'ул. Матросова, 179а, Светлоград', N'Светлоград', '2022-02-18', 4, 28, 1979, 879.4),
(N'ул. Пушкина, 12, Светлоград', N'Светлоград', '2020-10-01', 5, 118, 1980, 5316.8),
(N'ул. Пушкина, 3а, Светлоград', N'Светлоград', '2018-12-01', 5, 48, 1990, 1007.9),
(N'ул. Ярмарочная, 21, Светлоград', N'Светлоград', '2021-01-01', 5, 58, 1985, 2586.6);
GO

-- Insert Apartments for building 'ул. Матросова, 179а, Светлоград' (28 apartments)
DECLARE @building_id INT;
SELECT @building_id = building_id FROM Buildings WHERE address = N'ул. Матросова, 179а, Светлоград';

INSERT INTO Apartments (building_id, apartment_number)
SELECT @building_id, number
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
             (11),(12),(13),(14),(15),(16),(17),(18),(19),(20),
             (21),(22),(23),(24),(25),(26),(27),(28)) AS apartments(number);
GO

-- Insert Owners from Отчет по оплате.xlsx
INSERT INTO Owners (full_name, phone_number)
VALUES 
(N'Шевченко Ольга Викторовна', NULL),
(N'Мазалова Ирина Львовна', '89185647218'),
(N'Семеняка Юрий Геннадьевич', NULL),
(N'Савельев Олег Иванович', '89287815445'),
(N'Габиец Игорь Леонидович', NULL),
(N'Бунин Эдуард Михайлович', NULL),
(N'Бахшиев Павел Иннокентьевич', NULL),
(N'Байчорова Агата Рустамовна', '89643324574'),
(N'Тюренкова Наталья Сергеевна', '89629987214'),
(N'Александров Петр Константинович', NULL),
(N'Мазалова Ольга Николаевна', NULL),
(N'Лапшин Виктор Романович', NULL),
(N'Гусев Семен Петрович', '89188601163'),
(N'Гладилина Вера Михайловна', NULL),
(N'Лукин Илья Федорович', '89634568714'),
(N'Петров Станислав Игоревич', '89189187845'),
(N'Филь Марина Федоровна', NULL),
(N'Михайлов Игорь Вадимович', NULL),
(N'Масюк Динара Викторовна', NULL),
(N'Мартыненко Александр Сергеевич', '89183215428'),
(N'Устьянцева Анна Станиславовна', NULL),
(N'Антоненко Дмитрий Игоревич', NULL),
(N'Любяшева Галина Аркадьевна', '89625674581'),
(N'Захарящев Денис Сергеевич', NULL),
(N'Третьяк Ярослава Викторовна', NULL),
(N'Бондарь Сергей Вадимович', NULL),
(N'Петраков Артем Сергеевич', NULL),
(N'Вальке Рита Владимировна', NULL);
GO

-- Create PropertyOwnership relationships (each owner owns corresponding apartment)
INSERT INTO PropertyOwnership (owner_id, apartment_id)
SELECT o.owner_id, a.apartment_id
FROM Owners o
INNER JOIN Apartments a ON a.apartment_number = o.owner_id
INNER JOIN Buildings b ON a.building_id = b.building_id
WHERE b.address = N'ул. Матросова, 179а, Светлоград'
AND o.owner_id <= 28;
GO

-- Insert Debts from Список задолженностей.xlsx
INSERT INTO Debts (apartment_id, report_date, water_debt, electricity_debt)
SELECT 
    a.apartment_id,
    '2025-03-01',
    CASE 
        WHEN a.apartment_number = 2 THEN 2455.2
        WHEN a.apartment_number = 4 THEN 14567.56
        WHEN a.apartment_number = 8 THEN 178451.00
        WHEN a.apartment_number = 9 THEN 56.12
        WHEN a.apartment_number = 13 THEN 0.14
        WHEN a.apartment_number = 15 THEN 438.87
        WHEN a.apartment_number = 16 THEN 7837.45
        WHEN a.apartment_number = 20 THEN 1.27
        WHEN a.apartment_number = 23 THEN 102.89
        ELSE 0
    END,
    CASE 
        WHEN a.apartment_number = 2 THEN 7541.81
        WHEN a.apartment_number = 4 THEN 48517.25
        WHEN a.apartment_number = 8 THEN 257891.10
        WHEN a.apartment_number = 9 THEN 142.35
        WHEN a.apartment_number = 13 THEN NULL
        WHEN a.apartment_number = 15 THEN 1250.94
        WHEN a.apartment_number = 16 THEN 18991.79
        WHEN a.apartment_number = 20 THEN 0.84
        WHEN a.apartment_number = 23 THEN 387.58
        ELSE 0
    END
FROM Apartments a
INNER JOIN Buildings b ON a.building_id = b.building_id
WHERE b.address = N'ул. Матросова, 179а, Светлоград'
AND a.apartment_number IN (2, 4, 8, 9, 13, 15, 16, 20, 23);
GO

-- Insert Payments from Отчет по оплате.xlsx
INSERT INTO Payments (apartment_id, period_month, period_year, amount_charged, amount_paid)
SELECT 
    a.apartment_id,
    3, -- March
    2025,
    CASE a.apartment_number
        WHEN 1 THEN 3725.84
        WHEN 2 THEN 2914.56
        WHEN 3 THEN 4210.48
        WHEN 4 THEN 3712.88
        WHEN 5 THEN 1247.23
        WHEN 6 THEN 5714.12
        WHEN 7 THEN 3814.56
        WHEN 8 THEN 5946.54
        WHEN 9 THEN 7982.78
        WHEN 10 THEN 4328.84
        WHEN 11 THEN 3745.01
        WHEN 12 THEN 6748.12
        WHEN 13 THEN 7184.15
        WHEN 14 THEN 4879.69
        WHEN 15 THEN 3478.29
        WHEN 16 THEN 2841.48
        WHEN 17 THEN 6871.15
        WHEN 18 THEN 4982.15
        WHEN 19 THEN 5874.15
        WHEN 20 THEN 4318.95
        WHEN 21 THEN 5127.48
        WHEN 22 THEN 4986.61
        WHEN 23 THEN 3748.52
        WHEN 24 THEN 8120.45
        WHEN 25 THEN 1244.67
        WHEN 26 THEN 5486.45
        WHEN 27 THEN 2486.98
        WHEN 28 THEN 3475.98
    END,
    CASE 
        WHEN a.apartment_number IN (1, 3, 5, 6, 7, 10, 11, 12, 14, 17, 18, 19, 21, 22, 24, 25, 26, 27, 28) 
        THEN CASE a.apartment_number
            WHEN 1 THEN 3725.84
            WHEN 3 THEN 4210.48
            WHEN 5 THEN 1247.23
            WHEN 6 THEN 5714.12
            WHEN 7 THEN 3814.56
            WHEN 10 THEN 4328.84
            WHEN 11 THEN 3745.01
            WHEN 12 THEN 6748.12
            WHEN 14 THEN 4879.69
            WHEN 17 THEN 6871.15
            WHEN 18 THEN 4982.15
            WHEN 19 THEN 5874.15
            WHEN 21 THEN 5127.48
            WHEN 22 THEN 4986.61
            WHEN 24 THEN 8120.45
            WHEN 25 THEN 1244.67
            WHEN 26 THEN 5486.45
            WHEN 27 THEN 2486.98
            WHEN 28 THEN 3475.98
        END
        ELSE 0
    END
FROM Apartments a
INNER JOIN Buildings b ON a.building_id = b.building_id
WHERE b.address = N'ул. Матросова, 179а, Светлоград';
GO

-- Insert Employees for service requests
INSERT INTO Employees (full_name, position, phone_number, email)
VALUES 
(N'Иванов Петр Сергеевич', N'Сантехник', '89181112233', 'ivanov@company.ru'),
(N'Сидорова Мария Ивановна', N'Электрик', '89182223344', 'sidorova@company.ru'),
(N'Петров Алексей Викторович', N'Слесарь', '89183334455', 'petrov@company.ru'),
(N'Козлова Анна Дмитриевна', N'Уборщик', '89184445566', 'kozlova@company.ru'),
(N'Смирнов Дмитрий Алексеевич', N'Администратор', '89185556677', 'smirnov@company.ru');
GO

-- Insert ServiceRequests (примеры заявок)
INSERT INTO ServiceRequests (apartment_id, employee_id, request_type, description, status)
SELECT 
    a.apartment_id,
    e.employee_id,
    CASE 
        WHEN e.position = N'Сантехник' THEN N'Ремонт сантехники'
        WHEN e.position = N'Электрик' THEN N'Ремонт электрики'
        ELSE N'Обслуживание'
    END,
    CASE a.apartment_number
        WHEN 2 THEN N'Протечка воды в ванной'
        WHEN 4 THEN N'Не работает розетка на кухне'
        WHEN 8 THEN N'Замена лампочек в подъезде'
        WHEN 9 THEN N'Чистка канализационных стоков'
        WHEN 13 THEN N'Проверка отопительной системы'
        WHEN 15 THEN N'Ремонт двери подъезда'
        WHEN 16 THEN N'Уборка территории'
        WHEN 20 THEN N'Замена счетчика воды'
        WHEN 23 THEN N'Ремонт электропроводки'
    END,
    CASE 
        WHEN a.apartment_number IN (2, 4, 8) THEN N'Открыта'
        WHEN a.apartment_number IN (9, 13) THEN N'В работе'
        WHEN a.apartment_number IN (15, 16, 20, 23) THEN N'Закрыта'
    END
FROM Apartments a
CROSS JOIN Employees e
INNER JOIN Buildings b ON a.building_id = b.building_id
WHERE b.address = N'ул. Матросова, 179а, Светлоград'
AND a.apartment_number IN (2, 4, 8, 9, 13, 15, 16, 20, 23)
AND e.employee_id <= 3;
GO