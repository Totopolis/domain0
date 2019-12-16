insert into dom.Environment
([Name], [Description], [Token], [IsDefault])
values
('DefaultEnvironment','', 'kNOzmFJxIgqkvHU9S4jIq6zU39M9r+v2pcFRgi1SPsE=', 1)

go

declare @defaultEnvironment int;
set @defaultEnvironment = (select top 1 Id from dom.Environment where IsDefault = 1)

insert into dom.Message 
([Type], [Locale], [Name], [Template], [EnvironmentId])
values
('sms',		'en',		'WelcomeTemplate',				'Hello {0}!', @defaultEnvironment),
('sms',		'en',		'RegisterTemplate',				'Your password is: {0} will valid for {1} min', @defaultEnvironment),
('sms',		'en',		'RequestResetTemplate',			'Your NEW password is: {0} will valid for {1} min', @defaultEnvironment),
('sms',		'en',		'ForcePasswordResetTemplate',	'Your NEW password is: {0}', @defaultEnvironment),
('sms',		'en',		'RequestPhoneChangeTemplate',	'Your phone change pin is: {0}', @defaultEnvironment),

('sms',		'ru',		'WelcomeTemplate',				'Добро пожаловать {0}!', @defaultEnvironment),
('sms',		'ru',		'RegisterTemplate',				'Ваш пароль: {0} действителен {1} мин', @defaultEnvironment),
('sms',		'ru',		'RequestResetTemplate',			'Ваш НОВЫЙ пароль: {0} действителен {1} мин', @defaultEnvironment),
('sms',		'ru',		'ForcePasswordResetTemplate',	'Ваш НОВЫЙ пароль: {0}', @defaultEnvironment),
('sms',		'ru',		'RequestPhoneChangeTemplate',	'Ваш PIN для смены пароля: {0}', @defaultEnvironment),

('email',	'en',		'WelcomeTemplate',				'Hello {0}!', @defaultEnvironment),
('email',	'en',		'WelcomeSubjectTemplate',		'Hello {0}!', @defaultEnvironment),
('email',	'en',		'RegisterTemplate',				'Your password is: {0} will valid for {1} min', @defaultEnvironment),
('email',	'en',		'RegisterSubjectTemplate',		'Dear {0}! Welcome to {1}', @defaultEnvironment),
('email',	'en',		'RequestResetTemplate',			'Your NEW password is: {0} will valid for {1} min', @defaultEnvironment),
('email',	'en',		'RequestResetSubjectTemplate',	'{0}.Change password for {1}', @defaultEnvironment),
('email',	'en',		'ForcePasswordResetTemplate',	'Your NEW password is: {0}', @defaultEnvironment),
('email',	'en',		'ForcePasswordResetSubjectTemplate', '{0}! NEW password for {1}', @defaultEnvironment),
('email',	'en',		'RequestEmailChangeTemplate',	'Your email change pin is: {0}', @defaultEnvironment),
('email',	'en',		'RequestEmailChangeSubjectTemplate',	'Email change confirmation', @defaultEnvironment),

('email',	'ru',		'WelcomeTemplate',				'Добро пожаловать {0}!', @defaultEnvironment),
('email',	'ru',		'WelcomeSubjectTemplate',		'Добро пожаловать {0}!', @defaultEnvironment),
('email',	'ru',		'RegisterTemplate',				'Ваш пароль: {0} действителен {1} мин', @defaultEnvironment),
('email',	'ru',		'RegisterSubjectTemplate',		'{0}! Добро пожаловать в {1}', @defaultEnvironment),
('email',	'ru',		'RequestResetTemplate',			'Ваш НОВЫЙ пароль: {0} действителен {1} мин', @defaultEnvironment),
('email',	'ru',		'RequestResetSubjectTemplate',	'{0}. Изменение пароля для {1}', @defaultEnvironment),
('email',	'ru',		'ForcePasswordResetTemplate',	'Ваш НОВЫЙ пароль: {0}', @defaultEnvironment),
('email',	'ru',		'ForcePasswordResetSubjectTemplate',	'{0}! Новый пароль для {1}', @defaultEnvironment),
('email',	'ru',		'RequestEmailChangeTemplate',	'Ваш PIN для смены email: {0}', @defaultEnvironment),
('email',	'ru',		'RequestEmailChangeSubjectTemplate',	'Подтверждение смены email', @defaultEnvironment)

go