CREATE USER 'bond'@'localhost';
SET PASSWORD FOR 'bond'@'localhost' = PASSWORD('bond1');
GRANT Select, Delete, Insert, Trigger, Event, Execute, Alter Routine, Create Routine, Show View, Create View, Lock Tables, Create Temporary Tables, Alter, Index, References, Grant Option, Drop, Create, Update ON bondsmen.* TO 'bond'@'localhost';
