-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/14/2018 12:15:04 AM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.criminal_history(
  Case_GUID VARCHAR (32) NOT NULL,
  Case_Num_Status VARCHAR (128) NOT NULL,
  Defendant VARCHAR (128) DEFAULT NULL,
  Date_Filed DATE DEFAULT NULL,
  Date_Booked DATE DEFAULT NULL,
  Court VARCHAR (16) DEFAULT NULL,
  Defendant_Status VARCHAR (64) DEFAULT NULL,
  Disposition VARCHAR (128) DEFAULT NULL,
  Bond_Amount DECIMAL (10, 2) DEFAULT NULL,
  Offense VARCHAR (64) DEFAULT NULL,
  Next_Setting DATE DEFAULT NULL,
  INDEX criminal_history_FK1 USING BTREE (Case_GUID),
  CONSTRAINT criminal_history_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


