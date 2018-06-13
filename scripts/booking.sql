-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/14/2018 12:05:14 AM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.booking(
  Case_GUID VARCHAR (32) NOT NULL,
  Arrest_Date DATETIME DEFAULT NULL,
  Arrest_Location VARCHAR (64) DEFAULT NULL,
  Booking_Date DATETIME DEFAULT NULL,
  INDEX booking_FK1 USING BTREE (Case_GUID),
  CONSTRAINT booking_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


