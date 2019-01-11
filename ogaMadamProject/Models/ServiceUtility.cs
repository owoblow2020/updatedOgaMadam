﻿using AutoMapper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using ogaMadamProject.Controllers;
using ogaMadamProject.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;

namespace ogaMadamProject.Models
{
    public class ServiceUtility : IDisposable
    {
        bool disposed;
        private ApplicationDbContext _db;
        private OgaMadamAdo _db2;
        private Ado1 _db3;
       


        public ServiceUtility()
        {
            _db = new ApplicationDbContext();
            _db2 = new OgaMadamAdo();
            _db3 = new Ado1();
        }

        public Task<IEnumerable<AspNetUserDto>> ListUsers()
        {
            return Task.Run(() =>
            {
                System.Threading.Thread.Sleep(1000);
                return _db2.AspNetUsers.ToList().Select(Mapper.Map<AspNetUser, AspNetUserDto>);
            });
        }

        public Task<IEnumerable<CategoryDto>> ListCategory()
        {
            return Task.Run(() =>
            {
                System.Threading.Thread.Sleep(1000);
                return _db2.Categories.ToList().Select(Mapper.Map<Category, CategoryDto>);
            });
        }

        public bool VerifyBVN(string bvn)
        {
            return false;
        }

        public Task<bool> RegisterEmployee(RegisterModel model, string id)
        {
            return Task.Run(() =>
            {
                System.Threading.Thread.Sleep(1000);
                var employeeDetails = new Employee()
                {
                    EmployeeId = id,
                    BVN = model.ExtraData.BVN,
                    NIMC = model.ExtraData.NIMC,
                    CreatedAt = DateTime.Now
                    
                };

                switch (model.ExtraData.QualificationType)
                {
                    case "Bsc":
                        employeeDetails.QualificationType = QualificationType.Bsc;
                        break;
                    case "Hnd":
                        employeeDetails.QualificationType = QualificationType.Hnd;
                        break;
                    case "Msc":
                        employeeDetails.QualificationType = QualificationType.Msc;
                        break;
                    case "Ond":
                        employeeDetails.QualificationType = QualificationType.Ond;
                        break;
                    default:
                        employeeDetails.QualificationType = QualificationType.Ssce;
                        break;
                }

                _db2.Employees.Add(employeeDetails);
                if (_db2.SaveChanges() == 1)
                {
                    var content = new EmailSmsRequest()
                    {
                        From = "Oga Madam",
                        Message = "Email confirmation",
                        RecieptEmail = model.Email,
                        SenderEmail = model.Email,
                        Subject = "Email confirmation"
                    };

                    SendEmailSms(content);

                    //send phone verification

                    return true;
                }
                return false;
            });
        }

        public bool VerifyEmail(string id, string hashParam)
        {
            var user = _db2.AspNetUsers.FirstOrDefault(o => o.Id == id);
            var hashKey = Crypto.Hash(user.PhoneNumber + user.Email + user.FirstName);
            if (hashKey.Equals(hashParam))
            {
                user.IsEmailVerified = true;
                _db2.SaveChanges();
                return true;
            }
            return false;
        }

        public Task<string> SendEmailSms(EmailSmsRequest dataRequest)
        {
            try
            {
                if (! string.IsNullOrEmpty(dataRequest.RecieptEmail))
                {
                    //send email
                }

                if (!string.IsNullOrEmpty(dataRequest.Phone))
                {
                    //send sms

                }
            }
            catch (Exception)
            {

                throw;
            }
            return null;
        }

        public string RandomNumber()
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            string rNum = DateTime.Now.Millisecond + rnd.Next(0, 900000000).ToString();

            return rNum;
        }

        public ResponseModel EmployeeLoginAsync(EmployeeLoginDto requestParam)
        {
            ResponseModel res = new ResponseModel();
            var userStore = new UserStore<ApplicationUser>(_db);
            var manager = new UserManager<ApplicationUser>(userStore);

            var result = manager.Find(requestParam.Email, requestParam.Password);
            var employeeData = _db2.Employees.FirstOrDefault(o=>o.EmployeeId == result.Id);
            var uploadInfo = _db2.Uploads.Where(o => o.UploadId == result.Id).ToList();


            //check if user login successfully
            if (result == null)
            {
                return res;
            }
            var roleId = _db3.AspNetUserRoles.FirstOrDefault(o => o.UserId == result.Id);
            var role = _db2.AspNetRoles.FirstOrDefault(o => o.Id == roleId.RoleId);

            IList<UploadDto> uploadDtos = new List<UploadDto>();
            foreach (var item in uploadInfo)
            {
                var uploadId = new UploadDto()
                {
                    UploadId = item.UploadId
                };
                uploadDtos.Add(uploadId);
            }

            //check if account is activated
            if (result.AccountStatus == StatusType.Pending)
            {
                res.Data = "pending";
                return res;
            }

            var user = new EmployeeLoginDto()
            {
                Address = result.Address,
                DateOfBirth = result.DateOfBirth,
                Email = result.Email,
                FirstName = result.FirstName,
                LastName = result.LastName,
                MiddleName = result.MiddleName,
                Password = requestParam.Password,
                PhoneNumber = result.PhoneNumber,
                PlaceOfBirth = result.PlaceOfBirth,
                StateOfOrigin = result.StateOfOrigin,
                
            };
            user.Upload = uploadDtos;
            if (employeeData != null)
            {
                user.BVN = employeeData.BVN;
                user.NIMC = employeeData.NIMC;
            }
            if (result.Sex == SexType.Male)
            {
                user.Sex = "Male";
            }
            else
            {
                user.Sex = "Female";
            }

            res.Data = user;
            return res;
            
        }

        public bool PayTransaction(TransactionDto requestParam)
        {
            var trans = new Transaction()
            {
                Amount = Convert.ToDecimal(requestParam.Amount),
                EmployeeId = requestParam.EmployeeId,
                EmployerId = requestParam.EmployerId,
                PaymentCategory = requestParam.PaymentCategory,
                TransactionDate = Convert.ToDateTime(requestParam.TransactionDate),
                TransactionId = requestParam.TransactionId,
                CreatedAt = DateTime.Now
            };

            switch (requestParam.PaymentChannel)
            {
                case "Web":
                    trans.PaymentChannel = PaymentChannelType.Web;
                    break;
                case "Pos":
                    trans.PaymentChannel = PaymentChannelType.Pos;
                    break;
                case "Cash":
                    trans.PaymentChannel = PaymentChannelType.Cash;
                    break;
                default:
                    break;
            }

            switch (requestParam.PaymentStatus)
            {
                case "Failed":
                    trans.PaymentStatus = PaymentStatus.Failed;
                    break;
                case "Successful":
                    trans.PaymentStatus = PaymentStatus.Successful;
                    break;
                case "pending":
                    trans.PaymentStatus = PaymentStatus.pending;
                    break;
                default:
                    break;
            }

            var transSave = _db2.Transactions.Add(trans);
            if (_db2.SaveChanges() == 1)
            {
                var salaryParam = new Salary()
                {
                    EmployeeId = requestParam.EmployeeId,
                    EmployerId = requestParam.EmployerId,
                    StartDate = Convert.ToDateTime(requestParam.StartDate),
                    EndDate = Convert.ToDateTime(requestParam.EndDate),
                    TotalAmount = Convert.ToDecimal(requestParam.Amount),
                    SalaryId = requestParam.TransactionId,
                    CreatedAt = DateTime.Now
                };
                _db2.Salaries.Add(salaryParam);
                _db2.SaveChanges();

                return true;
            }
            return false;

        }

        public IList<EmployeeDto> AttachedEmployee(string employerId)
        {
            IList<EmployeeDto> employDtoList = new List<EmployeeDto>();
            var employeeList = _db2.Employees.Where(o => o.EmployerId == employerId).ToList();
            if (employDtoList == null)
            {
                return null;
            }
            foreach (var item in employeeList)
            {
                var employ = new EmployeeDto()
                {
                    AccountName = item.AccountName,
                    AccountNumber = item.AccountNumber,
                    Address = item.AspNetUser.Address,
                    AttachedDate = item.AttachedDate,
                    BankName = item.BankName,
                    BVN = item.BVN,
                    DateOfBirth = item.AspNetUser.DateOfBirth.ToString(),
                    Email = item.AspNetUser.Email,
                    FirstName = item.AspNetUser.FirstName,
                    IsAttachedApproved = item.IsAttachedApproved,
                    LastName = item.AspNetUser.LastName,
                    MiddleName = item.AspNetUser.MiddleName,
                    PhoneNumber = item.AspNetUser.PhoneNumber,
                    Id = item.EmployeeId,
                    NIMC = item.NIMC,
                    PlaceOfBirth = item.AspNetUser.PlaceOfBirth,
                    StateOfOrigin = item.AspNetUser.StateOfOrigin,
                    SalaryAmount = item.SalaryAmount,
                    IsInterviewed = item.IsInterviewed,
                    IsTrained = item.IsTrained,
                    IsUserVerified = item.IsUserVerified
                     
                };
                switch (item.QualificationType)
                {
                    case QualificationType.Ssce:
                        employ.QualificationType = "Ssce";
                        break;
                    case QualificationType.Ond:
                        employ.QualificationType = "Ond";
                        break;
                    case QualificationType.Hnd:
                        employ.QualificationType = "Hnd";
                        break;
                    case QualificationType.Bsc:
                        employ.QualificationType = "Bsc";
                        break;
                    case QualificationType.Msc:
                        employ.QualificationType = "Msc";
                        break;
                    default:
                        break;
                }

                employDtoList.Add(employ);
            }

            return employDtoList;
        }

        public IList<SalaryDto> ListSalary()
        {
            IList<SalaryDto> salaryDtoList = new List<SalaryDto>();
            var salaries = _db2.Salaries.ToList();
            if (salaries == null)
            {
                return null;
            }
            foreach (var item in salaries)
            {
                var salary = new SalaryDto()
                {
                    Amount = item.TotalAmount.ToString(),
                    Employee = item.Employee.AspNetUser.FirstName,
                    Employer = item.Employer.AspNetUser.FirstName,
                    EndDate = item.EndDate.ToString(),
                    SalaryId = item.SalaryId,
                    StartDate = item.StartDate.ToString()
                };

                salaryDtoList.Add(salary);
            }

            return salaryDtoList;
        }

        public IEnumerable<TransactionDto> ListTransaction()
        {
            IList<TransactionDto> transDtoList = new List<TransactionDto>();
            var trans = _db2.Transactions.ToList();
            foreach (var item in trans)
            {
                var transDto = new TransactionDto()
                {
                  Amount = item.Amount.ToString(),
                  EmployeeId = getUserName(item.Employee.EmployeeId),
                  EmployerId = getUserName(item.Employer.EmployerId),
                  EndDate = item.Salary.EndDate.ToString(),
                  PaymentCategory = item.PaymentCategory,
                  StartDate = item.Salary.StartDate.ToString(),
                  TransactionDate = item.TransactionDate.ToString(),
                  TransactionId = item.TransactionId
                };

                switch (item.PaymentChannel)
                {
                    case PaymentChannelType.Web:
                        transDto.PaymentChannel = "Web";
                        break;
                    case PaymentChannelType.Pos:
                        transDto.PaymentChannel = "Pos";
                        break;
                    case PaymentChannelType.Cash:
                        transDto.PaymentChannel = "Cash";
                        break;
                    default:
                        break;
                }

                switch (item.PaymentStatus)
                {
                    case PaymentStatus.Failed:
                        transDto.PaymentStatus = "Failed";
                        break;
                    case PaymentStatus.Successful:
                        transDto.PaymentStatus = "Successful";
                        break;
                    case PaymentStatus.pending:
                        transDto.PaymentStatus = "pending";
                        break;
                    default:
                        break;
                }

                transDtoList.Add(transDto);
            }
            return transDtoList;
        }

        private string getUserName(string id)
        {
            var user = _db2.AspNetUsers.FirstOrDefault(o=>o.Id == id);
            return user.FirstName + " " + user.LastName;
        }

        public IList<EmployeeDto> ListEmployee()
        {
            var users = _db2.AspNetUsers.ToList();
            IList<EmployeeDto> userList = new List<EmployeeDto>();
            foreach (var user in users)
            {
                if (user.Employee != null)
                {
                    var list = new EmployeeDto()
                    {
                        Id = user.Id,
                        Address = user.Address,
                        DateOfBirth = user.DateOfBirth.ToString(),
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        MiddleName = user.MiddleName,
                        PhoneNumber = user.PhoneNumber,
                        PlaceOfBirth = user.PlaceOfBirth,
                        StateOfOrigin = user.StateOfOrigin,
                        AccountName = user.Employee.AccountName,
                        AccountNumber = user.Employee.AccountNumber,
                        AttachedDate = user.Employee.AttachedDate,
                        BankName = user.Employee.BankName,
                        BVN = user.Employee.BVN,
                        IsAttachedApproved = user.Employee.IsAttachedApproved,
                        IsInterviewed = user.Employee.IsInterviewed,
                        IsTrained = user.Employee.IsTrained,
                        IsUserVerified = user.Employee.IsUserVerified,
                        NIMC= user.Employee.NIMC,
                        SalaryAmount = user.Employee.SalaryAmount 
                    };

                    switch (user.Employee.QualificationType)
                    {
                        case QualificationType.Ssce:
                            list.QualificationType = "Ssce";
                            break;
                        case QualificationType.Ond:
                            list.QualificationType = "Ond";
                            break;
                        case QualificationType.Hnd:
                            list.QualificationType = "Hnd";
                            break;
                        case QualificationType.Bsc:
                            list.QualificationType = "Bsc";
                            break;
                        case QualificationType.Msc:
                            list.QualificationType = "Msc";
                            break;
                        default:
                            break;
                    }

                    if (user.Sex == SexType.Male)
                    {
                        list.Sex = "Male";
                    }
                    else
                    {
                        list.Sex = "Female";
                    }
                    userList.Add(list);
                }
            }
            return userList;

        }

        public IList<EmployeeDto> ListVerifyEmployee()
        {
            var users = _db2.AspNetUsers.ToList();
            IList<EmployeeDto> userList = new List<EmployeeDto>();
            foreach (var user in users)
            {
                if (user.Employee != null && user.Employee.IsUserVerified == true)
                {
                    var list = new EmployeeDto()
                    {
                        Id = user.Id,
                        Address = user.Address,
                        DateOfBirth = user.DateOfBirth.ToString(),
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        MiddleName = user.MiddleName,
                        PhoneNumber = user.PhoneNumber,
                        PlaceOfBirth = user.PlaceOfBirth,
                        StateOfOrigin = user.StateOfOrigin,

                        AccountName = user.Employee.AccountName,
                        AccountNumber = user.Employee.AccountNumber,
                        AttachedDate = user.Employee.AttachedDate,
                        BankName = user.Employee.BankName,
                        BVN = user.Employee.BVN,
                        IsAttachedApproved = user.Employee.IsAttachedApproved,
                        IsInterviewed = user.Employee.IsInterviewed,
                        IsTrained = user.Employee.IsTrained,
                        IsUserVerified = user.Employee.IsUserVerified,
                        NIMC = user.Employee.NIMC,
                        SalaryAmount = user.Employee.SalaryAmount
                    };

                    switch (user.Employee.QualificationType)
                    {
                        case QualificationType.Ssce:
                            list.QualificationType = "Ssce";
                            break;
                        case QualificationType.Ond:
                            list.QualificationType = "Ond";
                            break;
                        case QualificationType.Hnd:
                            list.QualificationType = "Hnd";
                            break;
                        case QualificationType.Bsc:
                            list.QualificationType = "Bsc";
                            break;
                        case QualificationType.Msc:
                            list.QualificationType = "Msc";
                            break;
                        default:
                            break;
                    }

                    if (user.Sex == SexType.Male)
                    {
                        list.Sex = "Male";
                    }
                    else
                    {
                        list.Sex = "Female";
                    }
                    userList.Add(list);
                }
            }
            return userList;

        }

        public IList<EmployerDto> ListEmployer()
        {
            var users = _db2.AspNetUsers.ToList();
            IList<EmployerDto> userList = new List<EmployerDto>();
            foreach (var user in users)
            {
                if (user.Employer != null)
                {
                    var list = new EmployerDto()
                    {
                        Id = user.Id,
                        Address = user.Address,
                        DateOfBirth = user.DateOfBirth.ToString(),
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        MiddleName = user.MiddleName,
                        PhoneNumber = user.PhoneNumber,
                        PlaceOfBirth = user.PlaceOfBirth,
                        StateOfOrigin = user.StateOfOrigin,

                        EmploymentIdNumber = user.Employer.EmploymentIdNumber,
                        NextOfKin = user.Employer.NextOfKin,
                        NextOfKinAddress = user.Employer.NextOfKinAddress,
                        NextOfKinPhoneNumber = user.Employer.NextOfKinPhoneNumber,
                        PlaceOfWork = user.Employer.PlaceOfWork,
                        Profession = user.Employer.Profession
                    };

                    if (user.Sex == SexType.Male)
                    {
                        list.Sex = "Male";
                    }
                    else
                    {
                        list.Sex = "Female";
                    }
                    userList.Add(list);
                }
            }
            return userList;

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //dispose managed resources
                    _db.Dispose();
                }
            }
            //dispose unmanaged resources
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}