-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/14/2018 12:34:39 AM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.hold(
  Case_GUID VARCHAR (32) DEFAULT NULL,
  Agency_Placing_Hold VARCHAR (64) DEFAULT NULL,
  Agency_Name VARCHAR (64) DEFAULT NULL,
  Warrant_Number VARCHAR (64) DEFAULT NULL,
  Bond_Amount DECIMAL (10, 2) DEFAULT NULL,
  Offense VARCHAR (64) DEFAULT NULL,
  Placed_Date DATE DEFAULT NULL,
  Lifted_Date DATE DEFAULT NULL,
  INDEX hold_FK1 USING BTREE (Case_GUID),
  CONSTRAINT hold_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


