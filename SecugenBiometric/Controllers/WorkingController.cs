using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SecuGen.FDxSDKPro.Windows;
using System.Data;
using System.Data.SqlClient;



namespace SecugenBiometric.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkingController : ControllerBase
    {
        private readonly DbContext _dbContext;
        private readonly FingerPrintServices _fingerPrintServices;
        public WorkingController(DbContext dbContext)
        {
            _dbContext = dbContext;
            _fingerPrintServices = new FingerPrintServices();
        }

        [HttpGet("ScanFingerprint")]
        public async Task<IActionResult> ScanFingerPrint(string UserId)
        {
            try
            {
                byte[] fingerPrintImage = await _fingerPrintServices.CaptureFingerPrint();

                byte[] scannedTempalte = await _fingerPrintServices.CreateFingerPrintTemplate(fingerPrintImage);

                string query = @"
                   Insert into user_biometric (FingerprintData, CreatedAt, UserId)
                   Values (@FingerPrintData, @CreatedAt, @UserId)";

                using (var con = this._dbContext.CreateConnection())
                {
                    using (var sqlcon = (SqlConnection)con)
                    {
                        sqlcon.Open();
                        using (SqlCommand cmd = new SqlCommand(query, sqlcon))
                        {
                            cmd.Parameters.AddWithValue("@UserId", UserId);
                            cmd.Parameters.AddWithValue("@FingerPrintData", scannedTempalte);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                            cmd.ExecuteNonQuery();
                        }


                    }

                }
                return Ok(scannedTempalte);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            finally
            {
                _fingerPrintServices.Close();
            }




        }


        [HttpGet("CompareFingerPrint")]
        public async Task<IActionResult> CompareFingerPrint(string _userId)
        {
            try
            {
                // Retrieve stored fingerprint template
                byte[] storedFingerPrint = null;
                using (var con = _dbContext.CreateConnection())
                {
                    string query = "SELECT FingerPrintData FROM user_biometric WHERE UserId = @UserId";
                    storedFingerPrint = con.QueryFirstOrDefault<byte[]>(query, new { UserId = _userId }, commandType: CommandType.Text);
                }

                if (storedFingerPrint == null)
                {
                    return NotFound(new { message = "No fingerprint data found for the user." });
                }

                // Capture and process new fingerprint
                byte[] newFingerPrintImage = await _fingerPrintServices.CaptureFingerPrint();
                byte[] newScannedTemplate = await _fingerPrintServices.CreateFingerPrintTemplate(newFingerPrintImage);

    
           

                // Compare fingerprints
                SGFPMSecurityLevel securityLevel = SGFPMSecurityLevel.LOW;

                bool matched = false;
                SGFingerPrintManager _fingerPrintManager = new SGFingerPrintManager();
                int errorCode = _fingerPrintManager.MatchTemplate(storedFingerPrint, newScannedTemplate, securityLevel, ref matched);

                if (errorCode != (int)SGFPMError.ERROR_NONE)
                {
                    return BadRequest(new { message = $"Fingerprint comparison failed. Error code: {errorCode}" });
                }

                if (matched)
                {
                    return Ok(new { message = "Fingerprint matched successfully." });
                }
                else
                {
                    return Unauthorized(new { message = "Fingerprint did not match." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            finally
            {
                _fingerPrintServices.Close();
            }
        }




    }
}
