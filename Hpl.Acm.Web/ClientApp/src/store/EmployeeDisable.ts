import { Action, Reducer } from "redux";
import { AppThunkAction } from ".";
import { IEmployeeDisable } from "../models/Employee";

// -----------------
// STATE - This defines the type of data maintained in the Redux store.
export interface IEmployeesDisableState {
  isLoading: boolean;
  startDateIndex?: number;
  employeesDis: IEmployeeDisable[];
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.

interface IRequestEmployeesDisableAction {
  type: "REQUEST_EMPLOYEES_DISABLE";
  startDateIndex: number;
}

interface IReceiveEmployeesDisableAction {
  type: "RECEIVE_EMPLOYEES_DISABLE";
  startDateIndex: number;
  employeesDis: IEmployeeDisable[];
}

// Declare a 'discriminated union' type. This guarantees that all references to 'type' properties contain one of the
// declared type strings (and not any other arbitrary string).
type KnownAction =
  | IRequestEmployeesDisableAction
  | IReceiveEmployeesDisableAction;

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
  requestEmployees:
    (startDateIndex: number): AppThunkAction<KnownAction> =>
    (dispatch, getState) => {
      // Only load data if it's something we don't already have (and are not already loading)
      const appState = getState();
      if (
        appState &&
        appState.employeesOut &&
        startDateIndex !== appState.employeesOut.startDateIndex
      ) {
        // fetch(`https://localhost:44352/api/abphpl/GetAllLogNhanVien`)
        fetch(`api/employee/GetAllNhanVienNghiViecDaDisable`)
          .then((response) => {
            return response.json() as Promise<IEmployeeDisable[]>;
          })
          .then((data) => {
            // console.log(data.Payload);
            dispatch({
              type: "RECEIVE_EMPLOYEES_DISABLE",
              startDateIndex: startDateIndex,
              employeesDis: data,
            });
          });

        dispatch({
          type: "REQUEST_EMPLOYEES_DISABLE",
          startDateIndex: startDateIndex,
        });
      }
    },
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.

const unloadedState: IEmployeesDisableState = {
  employeesDis: [],
  isLoading: false,
};

export const reducer: Reducer<IEmployeesDisableState> = (
  state: IEmployeesDisableState | undefined,
  incomingAction: Action
): IEmployeesDisableState => {
  if (state === undefined) {
    return unloadedState;
  }

  const action = incomingAction as KnownAction;
  switch (action.type) {
    case "REQUEST_EMPLOYEES_DISABLE":
      return {
        startDateIndex: action.startDateIndex,
        employeesDis: state.employeesDis,
        isLoading: true,
      };
    case "RECEIVE_EMPLOYEES_DISABLE":
      // Only accept the incoming data if it matches the most recent request. This ensures we correctly
      // handle out-of-order responses.
      if (action.startDateIndex === state.startDateIndex) {
        return {
          startDateIndex: action.startDateIndex,
          employeesDis: action.employeesDis,
          isLoading: false,
        };
      }
      break;
  }

  return state;
};
