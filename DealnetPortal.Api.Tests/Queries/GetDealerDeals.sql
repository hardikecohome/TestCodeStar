SELECT               
                ACC.NAME as dealer_name, 
                Contract.ContractOid AS transaction#,    
                Contract.ContractId                       as Contract_id, 
                upper(ISNULL(ENTTY.name,''))   as Customer_name,
                ISNULL(STAT.descr,'')      as [Deal_Status],
                CONVERT(varchar,Contract.LastChangeDateTime,101)     as [Last_Update_Date],
                CONVERT(varchar,Contract.LastChangeDateTime,108)     as [Last_Update_Time],
 
                EQUIP.Description AS Equipment_Description,
                ISNULL(EQPTYPE.descr,'')as [Equipment_Type],
 
                CAST(ISNULL((select dbo.[GetContractAmountFinancedFN](contract.ContractOid)),0) AS numeric(11,2)) as [Amount Financed],
 
                ISNULL(CTYPE.data_value,'') AS [Contract_Type_Code],
                ISNULL(ENTTY.entt_id,'') AS [Customer ID],           
                
                               
                                                             ISNULL(ContractTerm.Term,0)     as [Term]
                                            
                  FROM Contract  (NOLOCK)
                                              LEFT JOIN ContractTerm (NOLOCK)
                                                             ON ContractTerm.ContractOid = Contract.ContractOid
                                                             AND ContractTerm.IsPrimary = 1
                                              LEFT JOIN Product (NOLOCK)
                                                             ON ContractTerm.ProductOid = Product.Oid
                                              LEFT OUTER JOIN Entity (NOLOCK) ENTTY
                                                             ON ENTTY.oid = Contract.EntityOid
                                              LEFT OUTER JOIN LTIValues (NOLOCK) CTYPE
                                                             ON CTYPE.oid = Contract.ContractTypeOid
                                              LEFT OUTER JOIN LTIValues (NOLOCK) FPRGM
                                                             ON FPRGM.oid = Contract.ProgramOid
                                              LEFT OUTER JOIN LTIValues (NOLOCK) POTYPE
                                                             ON POTYPE.oid = Contract.PurchaseOptionTypeOid
                                              LEFT OUTER JOIN Status (NOLOCK) STAT 
                                                             ON Contract.StatusOid = STAT.oid
                                              LEFT OUTER JOIN LPAcctDistCodeVW (NOLOCK) ADC
                                                             ON Contract.AccountDistributionCodeOid = ADC.oid
                                              LEFT OUTER JOIN dbo.LPTranCodeVW  (NOLOCK) TRNCD
                                                             ON Contract.TransactionCodeOid = TRNCD.oid 
                                              LEFT OUTER JOIN LPInvCodeVW (NOLOCK) INVCD 
                                                             ON INVCD.oid = Contract.InvoiceCodeOid
                                              LEFT OUTER JOIN lPDelinqCodeVW (NOLOCK) DELCD
                                                             ON DELCD.oid = Contract.DelinquencyCodeOid
                                              LEFT JOIN LTIValues (NOLOCK) SAL
                                                             ON ENTTY.Salutation = SAL.data_value
                                                             AND SAL.table_key = 'SALUTATION'
                                              LEFT JOIN LTIValues (NOLOCK) LtiSuffix
                                                             ON ENTTY.Suffix = LtiSuffix.data_value
                                                             AND LtiSuffix.table_key = 'SUFFIX'
                                              LEFT JOIN ChildEntity (NOLOCK) CHILD
                                                             ON CHILD.ref_oid = Contract.ContractOid 
                                                             AND CHILD.ref_type = 'CNTRCT'
                                                             AND CHILD.role_type = 'FNCOMP'
                                                             AND CHILD.is_primary <> 0
                                              LEFT JOIN Entity (NOLOCK) FNPGM
                                                             ON FNPGM.oid = CHILD.entt_oid
                                              LEFT OUTER JOIN 
                                                             (SELECT SHSUB.ref_oid, 
                                                                                           MIN(SHSUB.current_status_effective_date) AS [Date Quoted]
                                                             FROM StatusHistory (NOLOCK) SHSUB
                                                                            INNER JOIN Contract (NOLOCK) CNTRSUB
                                                                                           ON CNTRSUB.ContractOid = SHSUB.ref_oid
                                                                                           AND status_type = 'CONTRACT'
                                                             GROUP BY SHSUB.ref_oid            ) AS STATHIST
                                                             ON Contract.ContractOid= STATHIST.ref_oid        
                                              Left Join contract (NOLOCK) ParentContract on ParentContract.contractOid = contract.PurposeOfFinanceParentOid
                                              LEFT JOIN CreditDecision (NOLOCK)  ON 
                                                             Contract.CreditDecisionOid = CreditDecision.CreditDecisionOid
                                              LEFT OUTER JOIN ApplicationSetting (NOLOCK)
                                                  ON ApplicationSetting.appl_name = 'Auto Sequence' AND ApplicationSetting.setting = 'Contract ID Separator'          
                                              LEFT OUTER JOIN ContractPurposeOfFinance (NOLOCK) PrimaryContractPurposeOfFinance
                                                             ON Contract.ContractOid = PrimaryContractPurposeOfFinance.ContractOid AND PrimaryContractPurposeOfFinance.IsPrimary = 1
                                              LEFT JOIN LTIValues (NOLOCK) PrimaryPurposeOfFinance
                                                             ON PrimaryPurposeOfFinance.oid = PrimaryContractPurposeOfFinance.PurposeOfFinanceTypeOid
                                              LEFT OUTER JOIN ContractPurposeOfFinance (NOLOCK) AllContractPurposeOfFinance
                                                             ON Contract.ContractOid = AllContractPurposeOfFinance.ContractOid
                                              LEFT JOIN LTIValues (NOLOCK) AllPurposeOfFinance
                                                             ON AllPurposeOfFinance.oid = AllContractPurposeOfFinance.PurposeOfFinanceTypeOid
                    LEFT JOIN dbo.DocGenConContractTaxRatesAndAmounts (NOLOCK) DGTaxRates
                      ON DGTaxRates.ContractOid = Contract.ContractOid
 
                               LEFT JOIN ChildEntity (NOLOCK) CHILD1
                ON CHILD1.ref_oid = Contract.ContractOid 
                INNER JOIN ROLE (NOLOCK) RL ON CHILD1.role_oid = RL.oid  AND CHILD1.role_type = RL.role_type  
 
                inner JOIN DocGenAccAccount (NOLOCK) ACC
                                                                                                                                                                      ON ACC.oid = CHILD1.entt_oid
                                                                                                                        
                LEFT JOIN ContractEquipment (NOLOCK) CONEQ
                                                             ON Contract.ContractOid = CONEQ.ContractOid
                LEFT JOIN Equipment (NOLOCK) EQUIP
                                                             ON EQUIP.EquipmentOid = CONEQ.EquipmentOid
 
                                                                            LEFT OUTER JOIN EquipmentType EQPTYPE
                                                             ON EQPTYPE.oid = EQUIP.EquipmentTypeOid

 
                 where acc.[account id] in (SELECT  
 
                      en.entt_id
                  FROM SecurityUser (nolock) sc
                  inner join entity (nolock) en on sc.oid = en.secu_oid
                 where sc.user_id  LIKE '{0}%')
                 order by 2 desc;