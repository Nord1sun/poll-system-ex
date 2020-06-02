using iess_api.Interfaces;
using iess_api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iess_api.Managers
{
    public class ExceptionManager : IExceptionManager
    {
        private readonly IExceptionRepository _exceptionRepository;
        public ExceptionManager(IExceptionRepository exceptionRepository)
        {
            _exceptionRepository = exceptionRepository;
        }

        public async Task<List<ExceptionModel>> GetAllExceptions()
        {
            var exceptions = await _exceptionRepository.GetAllAsync();
            if (exceptions == null)
            {
                return null;
            }
            return exceptions;
        }
    }
}
