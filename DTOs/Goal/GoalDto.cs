namespace SmartFin.DTOs.Goal
{

    public class GoalDto
    {

        public int id { get; set; }


        public DateTime dateOfStart { get; set; }

        public DateTime dateOfEnd { get; set; }

        public decimal payment { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public decimal plannedSum { get; set; }

        public decimal currentSum { get; set; }

        public string status { get; set; }

        public List<int> UserId { get; set; }

        public decimal lastMonthContribution { get; set; }
        public DateTime lastContributionDate { get; set; }



    }
}