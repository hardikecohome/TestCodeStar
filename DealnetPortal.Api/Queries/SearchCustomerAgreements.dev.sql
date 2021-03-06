DECLARE @CUST_FIRST_NAME VARCHAR(50);
DECLARE @CUST_LAST_NAME VARCHAR(50);
DECLARE @CUST_DATE_OF_BIRTH AS DATETIME;
DECLARE @CUST_COMPANY AS VARCHAR(10);

SET @CUST_FIRST_NAME = '{0}'
SET @CUST_LAST_NAME ='{1}'
SET @CUST_DATE_OF_BIRTH = '{2}'
SET @CUST_COMPANY = '08'

/****** Script for SelectTopNRows command from SSMS  ******/
SELECT *
FROM [Aspire].[dbo].[Sample_CustomerExistingAgreements]
WHERE lease_cust_id_num =
(SELECT TOP 1 entt_id
FROM Entity
where fname  = @CUST_FIRST_NAME  and lname  = @CUST_LAST_NAME)