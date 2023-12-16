import { Action, Reducer } from "redux";
import { AppThunkAction } from ".";
import { IDepartmentAcm } from "../models/Department";

// -----------------
// STATE - This defines the type of data maintained in the Redux store.
export interface IDepartmentAcmState {
  isLoading: boolean;
  startDateIndex?: number;
  departmentAcms: IDepartmentAcm[];
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.

interface IRequestDepartmentAcmAction {
  type: "REQUEST_DEPARTMENT_ACM";
  startDateIndex: number;
}

interface IReceiveDepartmentAcmAction {
  type: "RECEIVE_DEPARTMENT_ACM";
  startDateIndex: number;
  departmentAcms: IDepartmentAcm[];
}

// Declare a 'discriminated union' type. This guarantees that all references to 'type' properties contain one of the
// declared type strings (and not any other arbitrary string).
type KnownAction = IRequestDepartmentAcmAction | IReceiveDepartmentAcmAction;

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
  requestDepartmentAcms:
    (startDateIndex: number): AppThunkAction<KnownAction> =>
    (dispatch, getState) => {
      console.log("test");
      // Only load data if it's something we don't already have (and are not already loading)
      const appState = getState();
      if (
        appState &&
        appState.departmentAcms &&
        startDateIndex !== appState.departmentAcms.startDateIndex
      ) {
        // fetch(`https://localhost:44332/api/department/GetAllDepartment`)
        fetch(`api/department/GetAllDepartment`)
          .then((response) => {
            console.log(response.json());
            return response.json() as Promise<IDepartmentAcm[]>;
          })
          .then((data) => {
            // console.log(data.Payload);
            dispatch({
              type: "RECEIVE_DEPARTMENT_ACM",
              startDateIndex: startDateIndex,
              departmentAcms: data,
            });
          });

        dispatch({
          type: "REQUEST_DEPARTMENT_ACM",
          startDateIndex: startDateIndex,
        });
      }
    },
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.

const unloadedState: IDepartmentAcmState = {
  departmentAcms: [],
  isLoading: false,
};

export const reducer: Reducer<IDepartmentAcmState> = (
  state: IDepartmentAcmState | undefined,
  incomingAction: Action
): IDepartmentAcmState => {
  if (state === undefined) {
    return unloadedState;
  }

  const action = incomingAction as KnownAction;
  switch (action.type) {
    case "REQUEST_DEPARTMENT_ACM":
      console.log("REQUEST_DEPARTMENT_ACM");
      return {
        startDateIndex: action.startDateIndex,
        departmentAcms: state.departmentAcms,
        isLoading: true,
      };
    case "RECEIVE_DEPARTMENT_ACM":
      console.log("RECEIVE_DEPARTMENT_ACM");
      // Only accept the incoming data if it matches the most recent request. This ensures we correctly
      // handle out-of-order responses.
      if (action.startDateIndex === state.startDateIndex) {
        return {
          startDateIndex: action.startDateIndex,
          departmentAcms: action.departmentAcms,
          isLoading: false,
        };
      }
      break;
  }

  return state;
};
