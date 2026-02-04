using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ClubFundService : IClubFundService
    {
        private readonly IFundRepository _fundRepository;

        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_APPROVED = "APPROVED";
        private const string STATUS_REJECTED = "REJECTED";
        private const string TYPE_INCOME = "INCOME";
        private const string TYPE_EXPENSE = "EXPENSE";

        public ClubFundService(IFundRepository fundRepository)
        {
            _fundRepository = fundRepository;
        }

        public async Task<FundTransaction> CreateRequestAsync(Guid userId, CreateFundRequestDto request)
        {
            var fund = await _fundRepository.GetFundByIdAsync(request.FundId);
            if (fund == null) throw new Exception("Quỹ không tồn tại");

            var transaction = new FundTransaction
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                TransactionType = request.TransactionType.ToUpper(),
                Amount = request.Amount,
                Description = request.Description,
                TransactionDate = DateTime.UtcNow,
                Status = STATUS_PENDING, 
                CreatedBy = userId
            };

            await _fundRepository.AddTransactionAsync(transaction);
            return transaction;
        }

        public async Task<bool> ProcessRequestAsync(Guid managerId, ProcessFundRequestDto request)
        {
            var transaction = await _fundRepository.GetTransactionByIdAsync(request.TransactionId);

            if (transaction == null) throw new Exception("Giao dịch không tồn tại");
            if (transaction.Status != STATUS_PENDING) throw new Exception("Giao dịch đã được xử lý trước đó");

            var fund = transaction.ClubFund;
            transaction.ApprovedBy = managerId;
            transaction.TransactionDate = DateTime.UtcNow; 

            if (request.Action.ToUpper() == "APPROVE")
            {
                if (transaction.TransactionType == TYPE_EXPENSE)
                {
                    if (fund.CurrentBalance < transaction.Amount)
                    {
                        throw new Exception("Số dư quỹ không đủ để duyệt chi tiêu này");
                    }
                    fund.CurrentBalance -= transaction.Amount;
                }
                else if (transaction.TransactionType == TYPE_INCOME)
                {
                    fund.CurrentBalance += transaction.Amount;
                    fund.TotalAmount += transaction.Amount;
                }

                transaction.Status = STATUS_APPROVED;

                await _fundRepository.UpdateClubFundAsync(fund);
            }
            else
            {
                transaction.Status = STATUS_REJECTED;
            }

            await _fundRepository.UpdateTransactionAsync(transaction);
            return true;
        }

        public async Task<IEnumerable<FundTransaction>> GetFundHistoryAsync(int fundId, string? status)
        {
            var normalizedStatus = status?.ToUpper();
            return await _fundRepository.GetTransactionsByFundIdAsync(fundId, normalizedStatus);
        }
    }
}