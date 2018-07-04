SELECT a.OID,A.SEQ_NUM,A.DESCR AS UDF_COLUMN_NAME,                                    
                                    B.descr as Field_Description,
                                    B.OID as Field_Oid,
                                    B.valid_value as SubmissionValue
                                      FROM GenericField (NOLOCK) A
                                      LEFT JOIN GenericFieldValidValue(NOLOCK) B ON A.oid = B.genf_oid
                                      and b.active = 1 and a.active = 1                                      
									  where a.ref_type = 'CNTRCT' and b.active = 1
											and replace(a.descr,' ','') LIKE '%{0}%'
                                      ORDER BY 3, 5;