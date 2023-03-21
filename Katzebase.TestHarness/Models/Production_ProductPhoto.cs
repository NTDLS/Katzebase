using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductPhoto
    {
        #region Properties
        private int _productPhotoID;
        public int ProductPhotoID
        {
            get
            {
                return this._productPhotoID;
            }
            set
            {
                if (this._productPhotoID != value)
                {
                    this._productPhotoID = value;
                }
            }
        }
        private byte[] _thumbNailPhoto;
        public byte[] ThumbNailPhoto
        {
            get
            {
                return this._thumbNailPhoto;
            }
            set
            {
                if (this._thumbNailPhoto != value)
                {
                    this._thumbNailPhoto = value;
                }
            }
        }
        private string _thumbnailPhotoFileName;
        public string ThumbnailPhotoFileName
        {
            get
            {
                return this._thumbnailPhotoFileName;
            }
            set
            {
                if (this._thumbnailPhotoFileName != value)
                {
                    this._thumbnailPhotoFileName = value;
                }
            }
        }
        private byte[] _largePhoto;
        public byte[] LargePhoto
        {
            get
            {
                return this._largePhoto;
            }
            set
            {
                if (this._largePhoto != value)
                {
                    this._largePhoto = value;
                }
            }
        }
        private string _largePhotoFileName;
        public string LargePhotoFileName
        {
            get
            {
                return this._largePhotoFileName;
            }
            set
            {
                if (this._largePhotoFileName != value)
                {
                    this._largePhotoFileName = value;
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
