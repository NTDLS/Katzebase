namespace Katzebase.TestHarness.Models
{
    public partial class Production_Illustration
    {
        #region Properties
        private int _illustrationID;
        public int IllustrationID
        {
            get
            {
                return this._illustrationID;
            }
            set
            {
                if (this._illustrationID != value)
                {
                    this._illustrationID = value;
                }
            }
        }
        private string _diagram;
        public string Diagram
        {
            get
            {
                return this._diagram;
            }
            set
            {
                if (this._diagram != value)
                {
                    this._diagram = value;
                }
            }
        }
        private DateTime _modifiedDate;
        public DateTime ModifiedDate
        {
            get
            {
                return this._modifiedDate;
            }
            set
            {
                if (this._modifiedDate != value)
                {
                    this._modifiedDate = value;
                }
            }
        }

        #endregion
    }
}
