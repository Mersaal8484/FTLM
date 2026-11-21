export const isDailyTank = (tank) =>
  tank?.tankRole === 2 || tank?.tankRole === 'Daily' || tank?.generatorId != null;
