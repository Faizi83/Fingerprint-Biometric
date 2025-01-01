using SecuGen.FDxSDKPro.Windows;

namespace SecugenBiometric
{
    public class FingerPrintServices
    {
        private SGFingerPrintManager _fingerPrintManager;
        public FingerPrintServices()
        {
            _fingerPrintManager = new SGFingerPrintManager();

            SGFPMDeviceName device_name;
            Int32 device_id;
            device_name = SGFPMDeviceName.DEV_AUTO;
            device_id = (Int32)(SGFPMPortAddr.USB_AUTO_DETECT);
            int result = _fingerPrintManager.Init(device_name);

            if (result != (int)SGFPMError.ERROR_NONE)
            {
                throw new Exception("Fingerprint Manager Initialization failed");
            }

            result = _fingerPrintManager.OpenDevice(device_id);

            if (result != (int)SGFPMError.ERROR_NONE)
            {
                throw new Exception("FingerPrint Device could not be opened");
            }


        }

        public async Task<byte[]> CaptureFingerPrint()
        {
            SGFPMDeviceInfoParam deviceinfo = new SGFPMDeviceInfoParam();

            int result = _fingerPrintManager.GetDeviceInfo(deviceinfo);
            if(result != (int)SGFPMError.ERROR_NONE)
            {
                throw new Exception("Error retrievin device information. Error code: " + result);
            }
            byte[] fpImageBuffer = new byte[deviceinfo.ImageWidth * deviceinfo.ImageHeight];

            result = _fingerPrintManager.GetImage(fpImageBuffer);

            if (result != (int)SGFPMError.ERROR_NONE)
            {
                throw new Exception("Error capturing Fingerprint. Error code: " + result);
            }

            return fpImageBuffer;

        }

        public async Task<byte[]> CreateFingerPrintTemplate(byte[] fingerPrintImage)
        {
            try
            {
                // Determine the maximum template size
                int templateSize = 0;
                int resultSize = _fingerPrintManager.GetMaxTemplateSize(ref templateSize);
                if (resultSize != (int)SGFPMError.ERROR_NONE)
                {
                    throw new Exception($"Error retrieving max template size. Error code: {resultSize}");
                }

                // Allocate the buffer based on the determined size
                byte[] templateBuffer = new byte[templateSize];


                // Define fingerprint information
                SGFPMFingerInfo fingerInfo = new SGFPMFingerInfo
                {
                    FingerNumber = 0, // Finger index (e.g., 1 for thumb)
                    ImageQuality = 100, // Quality (adjust as needed)
                    ImpressionType = 0, // Type of impression (e.g., 0 for live scan)
                    ViewNumber = 0 // For single capture, typically 0
                };

                // Populate additional fields if required by the SDK

                // Now pass the populated object

                // Create the fingerprint template
                int result = _fingerPrintManager.CreateTemplate(fingerInfo, fingerPrintImage, templateBuffer);
                if (result != (int)SGFPMError.ERROR_NONE)
                {
                    throw new Exception($"Error creating Fingerprint template. Error code: {result}");
                }

                // Return the successfully created template
                return templateBuffer;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while creating the fingerprint template: {ex.Message}", ex);
            }
        }



        public void Close()
        {
            _fingerPrintManager.CloseDevice();  
        }
    }
}
