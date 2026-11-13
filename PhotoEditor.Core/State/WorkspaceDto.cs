namespace PhotoEditor.Core.State
{
    public class WorkspaceDto
    {
        public List<PictureDto> Pictures  { get; set; }= new List<PictureDto>();
        public List<CollageDto> Collages { get; set; } = new List<CollageDto>();
        public int CurrentIndex { get; set; }
    }
}