using System;
using System.Collections.Generic;
using AutoMapper;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests
{
    [TestClass]
    public class MappingTest
    {
        [TestInitialize]
        public void Intialize()
        {
            DealnetPortal.Api.App_Start.AutoMapperConfig.Configure();
        }

        [TestMethod]
        public void AssertMapperConfiguration()
        {
            try
            {
                Mapper.AssertConfigurationIsValid();
            }
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }

        [TestMethod]
        public void ContractToContractDTOTest()
        {
            var contract = new Contract()
            {
                Id = 1,
                ContractState = ContractState.Started,
                PrimaryCustomer = new Customer()
                {
                    Id = 1,
                    FirstName = "FstName",
                    LastName = "LstName",
                    Locations = new List<Location>()
                    { 
                        new Location()
                        {
                            City = "Paris",
                            Id = 1
                        }
                    }
                },
                SecondaryCustomers = new List<Customer>()
                {
                    new Customer()
                    {
                        Id = 2,
                        FirstName = "FstName2",
                        LastName = "LstName2",
                        DateOfBirth = DateTime.Today,
                    }
                }
            };

            var contractDTO = Mapper.Map<ContractDTO>(contract);
            Assert.IsNotNull(contractDTO);
        }

        [TestMethod]
        public void ContractDTOToContractTest()
        {
            var contractDTO = new ContractDTO()
            {
                Id = 1,
                ContractState = ContractState.Started,
                PrimaryCustomer = new CustomerDTO()
                {
                    FirstName = "FstName",
                    LastName = "LstName",
                    DateOfBirth = DateTime.Today,
                    Id = 1,
                    //Locations = new List<LocationDTO>()
                    //{ 
                    //    new LocationDTO()
                    //    {
                    //        City = "Paris"
                    //    }
                    //}
                },
                SecondaryCustomers = new List<CustomerDTO>()
                {
                    new CustomerDTO()
                    {
                        FirstName = "FstName2",
                        LastName = "LstName2",
                        DateOfBirth = DateTime.Today,
                        Id = 2,
                    }
                }
            };

            var contract = Mapper.Map<Contract>(contractDTO);
            Assert.IsNotNull(contract);
        }
    }
}
