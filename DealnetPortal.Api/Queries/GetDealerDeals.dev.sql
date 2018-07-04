SELECT dealer_name, transaction#, Contract_id, 
       Customer_name, Deal_Status, Last_Update_Date, Last_Update_Time,
       Equipment_Description, Equipment_Type, [Amount Financed],
       Contract_Type_Code, [Customer ID], Term
FROM sample_mydeals(NOLOCK) sd                                      
where dealer_name LIKE '{0}%';         