import * as WeatherForecasts from "./WeatherForecasts";
import * as Counter from "./Counter";
import * as EmployeesLog from "./EmployeesLogAdd";
import * as EmployeesOut from "./EmployeeOut";
import * as EmployeesDisable from "./EmployeeDisable";
import * as DepartmentAcmStore from "./DepartmentAcm";

// The top-level state object
export interface ApplicationState {
  counter: Counter.CounterState | undefined;
  weatherForecasts: WeatherForecasts.WeatherForecastsState | undefined;
  employeesLog: EmployeesLog.IEmployeesLogState | undefined;
  employeesOut: EmployeesOut.IEmployeesOutState | undefined;
  employeesDisable: EmployeesDisable.IEmployeesDisableState | undefined;
  departmentAcms: DepartmentAcmStore.IDepartmentAcmState | undefined;
}

// Whenever an action is dispatched, Redux will update each top-level application state property using
// the reducer with the matching name. It's important that the names match exactly, and that the reducer
// acts on the corresponding ApplicationState property type.
export const reducers = {
  counter: Counter.reducer,
  weatherForecasts: WeatherForecasts.reducer,
  employeesLog: EmployeesLog.reducer,
  employeesOut: EmployeesOut.reducer,
  employeesDisable: EmployeesDisable.reducer,
  departmentAcm: DepartmentAcmStore.reducer,
};

// This type can be used as a hint on action creators so that its 'dispatch' and 'getState' params are
// correctly typed to match your store.
export interface AppThunkAction<TAction> {
  (dispatch: (action: TAction) => void, getState: () => ApplicationState): void;
}
