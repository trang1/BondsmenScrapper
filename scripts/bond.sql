-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/17/2018 11:54:16 AM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.bond(
  Case_Guid VARCHAR (32) NOT NULL,
  `Date` DATE NOT NULL,
  Type VARCHAR (64) NOT NULL,
  Description VARCHAR (512) DEFAULT NULL,
  SNU VARCHAR (32) DEFAULT NULL,
  INDEX bond_FK1 USING BTREE (Case_Guid),
  CONSTRAINT bond_FK1 FOREIGN KEY (Case_Guid)
  REFERENCES bondsman.case_summary (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


