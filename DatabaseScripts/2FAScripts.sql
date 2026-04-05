SELECT * FROM AspNetUsers

UPDATE AspNetUsers SET TwoFactorEnabled = 1; 

ALTER TABLE AspNetUsers 
ADD CONSTRAINT DF_TwoFactorEnabled 
DEFAULT 1 FOR TwoFactorEnabled;


UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE Id = 2


