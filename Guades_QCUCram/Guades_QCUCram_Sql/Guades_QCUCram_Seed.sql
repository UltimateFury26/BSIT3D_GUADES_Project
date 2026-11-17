USE Guades_QCUCram_DB;
GO

INSERT INTO Rooms (RoomName) VALUES
 ('Collab Room 1'),('Collab Room 2'),('Collab Room 3'),
 ('Collab Room 4'),('Collab Room 5'),('Collab Room 6');

INSERT INTO TimeSlots (StartTime, EndTime) VALUES
 ('08:00','09:00'),('09:00','10:00'),('10:00','11:00'),
 ('11:00','12:00'),('12:00','13:00'),('13:00','14:00'),
 ('14:00','15:00'),('15:00','16:00'),('16:00','17:00');

INSERT INTO Settings (SettingKey, SettingValue, Description) VALUES
 ('GraceMinutes','15','Minutes after slot start before an unclaimed booking is forfeited'),
 ('NoShowThreshold','3','Forfeitures before a student is restricted'),
 ('RestrictionDays','7','Days a restriction lasts'),
 ('AdvanceBookingDays','1','Booking must be exactly this many days ahead'),
 ('DevTimeOffsetMinutes','0','TESTING ONLY: shifts system clock by this many minutes');
GO