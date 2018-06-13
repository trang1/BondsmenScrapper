-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/14/2018 12:22:28 AM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.settings_info(
  Case_GUID VARCHAR (32) DEFAULT NULL,
  Bond_Docket_Time DATETIME DEFAULT NULL,
  Jail_Docket_Time DATETIME DEFAULT NULL,
  INDEX settings_info_FK1 USING BTREE (Case_GUID),
  CONSTRAINT settings_info_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


