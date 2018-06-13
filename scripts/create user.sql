CREATE USER 'bnd'@'localhost';
SET PASSWORD FOR 'bnd'@'localhost' = PASSWORD('bnd');
GRANT Select, Delete, Insert, Trigger, Event, Execute, Alter Routine, Create Routine, Show View, Create View, Lock Tables, Create Temporary Tables, Alter, Index, References, Grant Option, Drop, Create, Update ON bondsman.* TO 'bnd'@'localhost';
