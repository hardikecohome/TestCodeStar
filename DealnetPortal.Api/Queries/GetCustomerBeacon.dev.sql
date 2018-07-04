SELECT TOP 1000 [fname]
      ,[lname]
      ,[customer_id]
      ,[date_of_birth]
      ,[contractoid]
      ,[crtd_datetime]
      ,[lupd_user]
      ,[lupd_datetime]
      ,[creditoid]
      ,[Beacon]
  FROM [Aspire].[dbo].[Sample_CreditBureau]
  where fname = '{0}' and lname = '{1}'
  order by lupd_datetime  desc