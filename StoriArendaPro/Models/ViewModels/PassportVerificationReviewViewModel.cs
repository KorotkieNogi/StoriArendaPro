namespace StoriArendaPro.Models.ViewModels
{
    public class PassportVerificationReviewViewModel
    {
        public int VerificationId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }

        public string PassportSeria { get; set; }
        public string PassportNumber { get; set; }
        public string IssuedBy { get; set; }
        public string Propiska { get; set; }
        public string PlaceLive { get; set; }

        public string PassportPhotoFront { get; set; }
        public string PassportPhotoBack { get; set; }

        public string Status { get; set; }
        public string AdminNotes { get; set; }
    }
}
